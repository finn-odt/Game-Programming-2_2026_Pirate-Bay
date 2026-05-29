using System;
using System.Collections.Generic;
using GameEvents;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using EngineCursor = UnityEngine.Cursor;
using EngineCursorLockMode = UnityEngine.CursorLockMode;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(UIDocument))]
#if ENABLE_INPUT_SYSTEM 
[RequireComponent(typeof(PlayerInput))]
#endif
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private GameObject itemPrefab;
    
    [SerializeField] private Font pirateFont;
    
    public InventoryItemDatabaseSO itemDatabase;
    public float dragDistanceThreshold;

    // action for opening/closing the inventory [bindings are set in Awake()]
    //private readonly InputAction toggleInventoryAction = new("Toggle Inventory", InputActionType.Button);

    private static VisualElement panel;
    private static ScrollView listScroller;
    private static VisualElement list;
    private static Label emptyState;
    private static Label statusLabel;
    private static VisualElement details;
    private static bool isOpen;
    private static InventoryChangedEvent lastInventory;

    private struct Details
    {
        public Image Icon;
        public Label Name;
        public Label Description;
        public Label Quantity;
        public Button DropButton;
    }

    private static Details itemDetails;
    private static InventoryItemDataSO currentSelectedItem;
    private static VisualElement currentSelectedElement;
    private static int currentSelectedQuantity;

    private static GameStateChangedEvent.GameState currentGameState = GameStateChangedEvent.GameState.Intro;
    
    private static VisualElement dragGhost;
    private static VisualElement pendingDragElement;
    private static bool isDragging = false, mousePointerIsDown = false;
    private static Vector2 dragOffset, mousePointerDownPosition;
    private InventoryItemDataSO pendingDragItem;
    private int pendingDragQuantity;
    
#if ENABLE_INPUT_SYSTEM 
    private PlayerInput _playerInput;
#endif

    private void Awake()
    {
        if (document == null)
        {
            document = GetComponent<UIDocument>();
        }

        //toggleInventoryAction.AddBinding("<Keyboard>/tab");
        //toggleInventoryAction.AddBinding("<Gamepad>/leftShoulder");
    }

    private void OnEnable()
    {
        //toggleInventoryAction.Enable();
        //toggleInventoryAction.performed += OnToggleInventory;

        GameEventManager.AddListener<InventoryChangedEvent>(OnInventoryChanged);
        GameEventManager.AddListener<GameStateChangedEvent>(OnGameStateChange);
        GameEventManager.AddListener<CloseInventoryEvent>(OnExternalCloseCommand);
        GameEventManager.AddListener<OpenInventoryEvent>(OnExternalOpenCommand);
    }

    private void OnDisable()
    {
        GameEventManager.RemoveListener<InventoryChangedEvent>(OnInventoryChanged);
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
        GameEventManager.RemoveListener<CloseInventoryEvent>(OnExternalCloseCommand);
        GameEventManager.RemoveListener<OpenInventoryEvent>(OnExternalOpenCommand);

        //toggleInventoryAction.performed -= OnToggleInventory;
        //toggleInventoryAction.Disable();
    }

    private void OnExternalCloseCommand(CloseInventoryEvent gameEvent)
    {
        SetOpen(false);
        GameEventManager.Raise(new InventoryVisibilityChangeEvent(false));
    }

    private void OnExternalOpenCommand(OpenInventoryEvent gameEvent)
    {
        if (isOpen)
            return;
        
        SetOpen(true);
        GameEventManager.Raise(new InventoryVisibilityChangeEvent(true));
    }

    private void OnGameStateChange(GameStateChangedEvent gameEvent)
    {
        currentGameState = gameEvent.newState;
    }

    private void Start()
    {
        BindDocument();
        SetOpen(false);
        
#if ENABLE_INPUT_SYSTEM 
        _playerInput = GetComponent<PlayerInput>();
#else
        Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
    }
    
#if ENABLE_INPUT_SYSTEM
    public void OnToggleInventory(InputValue value)
    {
        Debug.Log("Hahahahah");
        // only openable in Play-Mode
        if (currentGameState != GameStateChangedEvent.GameState.Play && currentGameState != GameStateChangedEvent.GameState.Inventory)
            return;
        
        SetOpen(!isOpen);
        // isOpen is now set correctly, raise event for GameState
        GameEventManager.Raise(new InventoryVisibilityChangeEvent(isOpen));
    }
#endif

    /*private void OnToggleInventory(InputAction.CallbackContext context)
    {
        // only openable in Play-Mode
        if (currentGameState != GameStateChangedEvent.GameState.Play && currentGameState != GameStateChangedEvent.GameState.Inventory)
            return;
        
        SetOpen(!isOpen);
        // isOpen is now set correctly, raise event for GameState
        GameEventManager.Raise(new InventoryVisibilityChangeEvent(isOpen));
    }*/

    private void BindDocument()
    {
        VisualElement root = document.rootVisualElement;
        
        ApplyFontToAllTextElements(root, pirateFont);
        
        panel = root.Q<VisualElement>("inventory-panel");
        listScroller = root.Q<ScrollView>("inventory-scroll");
        list = root.Q<VisualElement>("inventory-grid");
        emptyState = root.Q<Label>("empty-state");
        statusLabel = root.Q<Label>("status-label");
        
        dragGhost = root.Q<VisualElement>("inventory-slot-drag-ghost");
        
        details = root.Q<VisualElement>("inventory-details");
        
        itemDetails.Icon = root.Q<Image>("inventory-details-icon");
        itemDetails.Name = root.Q<Label>("inventory-details-name");
        itemDetails.Description = root.Q<Label>("inventory-details-description");
        itemDetails.Quantity = root.Q<Label>("inventory-details-quantity");
        itemDetails.DropButton = root.Q<Button>("drop-button");

        if (listScroller != null)
        {
            listScroller.style.display = DisplayStyle.None;
            listScroller.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            listScroller.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        Button closeButton = root.Q<Button>("close-button");
        if (closeButton != null)
        {
            closeButton.clicked += () => SetOpen(false);
        }
    }
    
    private void ApplyFontToAllTextElements(VisualElement root, Font font)
    {
        // Query all elements that derive from TextElement (Label, Button text, etc.)
        foreach (var textElement in root.Query<TextElement>().ToList())
        {
            textElement.style.unityFont = font;
        }
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        // do or do not receive click events
        if (document != null)
        {
            document.enabled = isOpen;
        }
        
        if (panel != null)
        {
            panel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (isOpen)
        {
            RenderInventory(lastInventory);
        }
        else
        {
            currentSelectedItem = null;
            if(currentSelectedElement != null)
                currentSelectedElement.RemoveFromClassList("selected");
            currentSelectedElement = null;
        }
    }

    private void OnInventoryChanged(InventoryChangedEvent inventoryEvent)
    {
        lastInventory = inventoryEvent;
        if (isOpen)
        {
            RenderInventory(inventoryEvent);
        }
    }

    private void RenderInventory(InventoryChangedEvent inventoryEvent)
    {
        if (list == null)
        {
            return;
        }

        list.contentContainer.Clear();
        
        List<Inventory.InventoryListItem> items;
        if (inventoryEvent != null)
            items = inventoryEvent.items;
        else
            items = Inventory.Instance.GetCollectedItems();

        if (items == null || items.Count == 0)
        {
            if (emptyState != null)
            {
                emptyState.style.display = DisplayStyle.Flex;
            }

            listScroller.style.display = DisplayStyle.None;

            if (statusLabel != null)
            {
                statusLabel.text = "Capacity: 0/" + Inventory.Instance.inventoryCapacity;
            }

            return;
        }

        if (emptyState != null)
        {
            emptyState.style.display = DisplayStyle.None;
        }

        listScroller.style.display = DisplayStyle.Flex;
        
        // generate Left and Right Hand Equip Cells
        VisualElement[] equipCells = CreateEquipCells();
        foreach(VisualElement equipCell in equipCells)
            list.contentContainer.Add(equipCell);

        int totalItems = 0;
        for (int i = 0; i < items.Count; i++)
        {
            Inventory.InventoryListItem item = items[i];
            InventoryItemDataSO itemData = item.saveData;

            // update quantity of current selection for details
            if (itemData == currentSelectedItem && item.amount != currentSelectedQuantity)
                currentSelectedQuantity = item.amount;

            if (itemData == null)
            {
                continue;
            }

            totalItems += item.amount;
            
            VisualElement gridCell = CreateGridCell(itemData, item.amount);
            list.contentContainer.Add(gridCell);

            // set initial value for selected item
            if (i == 0 && currentSelectedItem == null)
                OnSelectItem(itemData, item.amount, gridCell);
        }
        
        UpdateDetails();

        if (statusLabel != null)
        {
            statusLabel.text = $"Capacity: {totalItems}/" + Inventory.Instance.inventoryCapacity;
        }
    }

    private VisualElement[] CreateEquipCells()
    {
        string[] classes = { "left-hand-equip-slot", "right-hand-equip-slot" };
        string[] titleLabels = { "Left Hand", "Right Hand" };

        VisualElement[] elements = new VisualElement[classes.Length];
        for(int i = 0; i < classes.Length; i++)
        {
            string className = classes[i];
            
            VisualElement element = new();
            element.AddToClassList(className);
            element.focusable = true;
            
            Label title = new();
            title.AddToClassList("equip-slot-title");
            title.text = titleLabels[i];
            element.Add(title);

            Image icon = new();
            icon.AddToClassList("inventory-slot-icon");
            icon.image = null;
            element.Add(icon);

            Label quantityLabel = new("");
            quantityLabel.AddToClassList("inventory-slot-quantity");
            element.Add(quantityLabel);

            Label hiddenID = new();
            hiddenID.AddToClassList("inventory-slot-hidden-id");
            hiddenID.text = "";
            element.Add(hiddenID);
            elements[i] = element;
        }

        return elements;
    }
    
    private VisualElement CreateGridCell(InventoryItemDataSO item, int quantity)
    {
        VisualElement element = new();
        element.AddToClassList("inventory-slot");
        element.tooltip = item.ItemName;
        element.focusable = true;
        /*element.RegisterCallback<ClickEvent>(evt =>
        {
            OnSelectItem(item, quantity, element);
        });*/

        Image icon = new();
        icon.AddToClassList("inventory-slot-icon");
        icon.image = item.Icon;
        element.Add(icon);

        Label quantityLabel = new($"x{quantity}");
        quantityLabel.AddToClassList("inventory-slot-quantity");
        element.Add(quantityLabel);

        Label hiddenID = new();
        hiddenID.AddToClassList("inventory-slot-hidden-id");
        hiddenID.text = item.ItemId;
        element.Add(hiddenID);

        SetEventsForDragAndDrop(element);

        return element;
    }
    
    private bool HasClassInSelfOrParents(VisualElement element, string className)
    {
        while (element != null)
        {
            if (element.ClassListContains(className))
                return true;

            element = element.parent;
        }

        return false;
    }

    void OnSelectItem(InventoryItemDataSO item, int quantity, VisualElement element)
    {
        currentSelectedItem = item;
        currentSelectedQuantity = quantity;
        
        if(currentSelectedElement != null)
            currentSelectedElement.RemoveFromClassList("selected");
        
        element.AddToClassList("selected");
        currentSelectedElement = element;

        UpdateDetails();
    }

    private void UpdateDetails()
    {
        if (currentSelectedElement == null || currentSelectedItem == null)
            return;
        
        // Set Details
        if (itemDetails.Icon != null)
            itemDetails.Icon.image = currentSelectedItem.Icon;
        if (itemDetails.Icon != null)
            itemDetails.Name.text = currentSelectedItem.ItemName;
        if (itemDetails.Icon != null)
            itemDetails.Description.text = currentSelectedItem.Description;
        if (itemDetails.Icon != null)
            itemDetails.Quantity.text = $"x{currentSelectedQuantity}";
        if (itemDetails.DropButton != null)
            itemDetails.DropButton.RegisterCallback<ClickEvent>(evt =>
            {
                DropItemIntoWorld(currentSelectedElement);  // drop one item of this kind
            });
    }

    private void SetEventsForDragAndDrop(VisualElement element)
    {
        if (element == null)
            return;
        
        element.RegisterCallback<PointerDownEvent>(evt =>
        {
            // left mouse button = primary pointer
            if (evt.button != 0 || isDragging)
                return;

            element.CapturePointer(evt.pointerId);
            
            // Get InventoryItemData instance for this element
            Label itemIdLabel = element.Q<Label>(className: "inventory-slot-hidden-id");
            Label quantityLabel = element.Q<Label>(className: "inventory-slot-quantity");
            if (itemIdLabel == null || quantityLabel == null)
                return;
            int quantity = Int32.Parse(quantityLabel.text.Remove(0, 1));
            string itemID = itemIdLabel.text;
            InventoryItemDataSO itemData = itemDatabase.GetItemById(itemID);
            
            mousePointerIsDown = true;
            mousePointerDownPosition = evt.position;
            pendingDragElement = element;
            pendingDragItem = itemData;
            pendingDragQuantity = quantity;

            //StartDragging(element, evt.position);
        });

        element.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!mousePointerIsDown || pendingDragElement != element)
                return;
            
            if (!isDragging)
            {
                float distance = Vector2.Distance(mousePointerDownPosition, evt.position);

                if (distance < dragDistanceThreshold)
                    return;

                OnSelectItem(pendingDragItem, pendingDragQuantity, element);
                StartDragging(element, mousePointerDownPosition);
            }

            UpdateDragging(element, evt.position);
        });

        element.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!mousePointerIsDown || pendingDragElement == null || pendingDragElement != element)
                return;

            element.ReleasePointer(evt.pointerId);

            if (isDragging)
            {
                StopDragging(element, evt.position); // validate target location here
            }
            else
            {
                OnSelectItem(pendingDragItem, pendingDragQuantity, element);
            }

            mousePointerIsDown = false;
            pendingDragElement = null;
            pendingDragItem = null;
            pendingDragQuantity = 0;
        });
    }

    private void StartDragging(VisualElement element, Vector2 position)
    {
        if (isDragging || pendingDragElement == null || dragGhost == null)
            return;
        
        isDragging = true;
        
        // Difference between mouse position and element's top-left corner.
        dragOffset = position - element.worldBound.position;  // use element (not dragGhost) to get correct offset

        element.style.display = DisplayStyle.None;  // deactivate real element in list
        
        dragGhost.style.display = DisplayStyle.Flex;  // activate drag ghost
        // Move the element above siblings while dragging.
        dragGhost.BringToFront();
        
        //copy element into drag ghost
        Image icon = element.Q<Image>(className: "inventory-slot-icon");
        Label quantity = element.Q<Label>(className: "inventory-slot-quantity");
        Image iconCopy = dragGhost.Q<Image>(className: "inventory-slot-icon");
        Label quantityCopy = dragGhost.Q<Label>(className: "inventory-slot-quantity");
        if (icon != null && quantity != null && iconCopy != null && quantityCopy != null)
        {
            iconCopy.image = icon.image;
            quantityCopy.text = quantity.text;
        }
        
        // Immediately move ghost to the correct position.
        UpdateDragging(element, position);
    }

    private void UpdateDragging(VisualElement element, Vector2 position)
    {
        if (!isDragging || pendingDragElement == null || pendingDragElement != element || dragGhost == null)
            return;

        // Convert mouse position from panel/world coordinates into parent's local coordinates.
        Vector2 localPointerPosition = dragGhost.parent.WorldToLocal(position);

        dragGhost.style.left = localPointerPosition.x - dragOffset.x;
        dragGhost.style.top = localPointerPosition.y - dragOffset.y;
    }

    private void StopDragging(VisualElement element, Vector2 position)
    {
        if (!isDragging || pendingDragElement == null || dragGhost == null)
            return;
        
        bool droppedInsideInventory = panel.worldBound.Contains(position);

        if (droppedInsideInventory)
        {
            TryReorderInsideInventory(element);
        }
        else
        {
            DropItemIntoWorld(element, true);
        }

        element.style.display = DisplayStyle.Flex;  // activate real element in list again
        dragGhost.style.display = DisplayStyle.None;  // deactivate drag ghost

        isDragging = false;
        pendingDragElement = null;
    }

    private void DropItemIntoWorld(VisualElement element, bool dropAll = false)
    {
        Label itemIdLabel = element.Q<Label>(className: "inventory-slot-hidden-id");
        Label quantityLabel = element.Q<Label>(className: "inventory-slot-quantity");
        if (itemIdLabel == null || quantityLabel == null)
            return;
        int quantity = Int32.Parse(quantityLabel.text.Remove(0, 1));
        string itemID = itemIdLabel.text;

        InventoryItemDataSO itemData = itemDatabase.GetItemById(itemID);
        
        GameObject droppedItem = Instantiate(itemPrefab, dropOrigin.position, Quaternion.identity);

        CollectableItem itemScript = droppedItem.GetComponentInChildren<CollectableItem>();
        itemScript.InventoryItemData = itemData;
        itemScript.amount = dropAll ? quantity : 1;
        
        if (!dropAll && quantity > 1)
            Inventory.Instance.Remove(itemData, 1);
        else
        {
            // reset selection if selected object is completely thrown out
            if (currentSelectedItem != null && currentSelectedItem == itemData)
            {
                currentSelectedItem = null;
                currentSelectedElement = null;
            }
            Inventory.Instance.Remove(itemData, quantity);
        }

        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;  // continuous detection for falling on ground
            Vector3 force = dropOrigin.forward.normalized * dropSpeed;
            rb.AddForce(force);
        }
    }
    
    [SerializeField] private float dropSpeed = 1.6f;
    [SerializeField] private Transform dropOrigin;
    
    private void TryReorderInsideInventory(VisualElement element)
    {
        throw new NotImplementedException("TryReorderInsideInventory of InventoryUI.cs");
    }
}
