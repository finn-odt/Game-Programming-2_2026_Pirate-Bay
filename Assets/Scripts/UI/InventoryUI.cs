using System;
using System.Collections.Generic;
using System.Linq;
using Configurations;
using GameEvents;
using SLTypes;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityServiceLocator;
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

    private IPlayer player;
    
    public InventoryItemDatabaseSO itemDatabase;
    public float dragDistanceThreshold;

    // action for opening/closing the inventory [bindings are set in Awake()]
    //private readonly InputAction toggleInventoryAction = new("Toggle Inventory", InputActionType.Button);

    private struct Hand
    {
        public VisualElement parent;
        public Image icon;
        public Label quantity;
        public Label itemId;
    }
    private struct HandElements
    {
        public VisualElement parent;
        public Hand leftHand;
        public Hand rightHand;
    }

    private static VisualElement root;
    private static VisualElement panel;
    private static ScrollView listScroller;
    private static VisualElement list;
    private static Label statusLabel;
    private static VisualElement details;
    private static HandElements hands;
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
        
        GameEventManager.AddListener<RemoveItemFromHandForUseEvent>(OnItemRemoveDueToUse);
        
        GameEventManager.AddListener<ToggleInventoryEvent>(OnToggleInventory);
    }

    private void OnDisable()
    {
        GameEventManager.RemoveListener<InventoryChangedEvent>(OnInventoryChanged);
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
        GameEventManager.RemoveListener<CloseInventoryEvent>(OnExternalCloseCommand);
        GameEventManager.RemoveListener<OpenInventoryEvent>(OnExternalOpenCommand);
        
        GameEventManager.RemoveListener<RemoveItemFromHandForUseEvent>(OnItemRemoveDueToUse);
        
        GameEventManager.RemoveListener<ToggleInventoryEvent>(OnToggleInventory);

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

        ServiceLocator.ForSceneOf(this).Get(out player);

        // load hand equipment
        GameConfiguration.GetHandEquipmentData(out string leftItemID, out string rightItemID, out int leftQuantity, out int rightQuantity);
        InventoryItemDataSO left = itemDatabase.GetItemById(leftItemID);
        InventoryItemDataSO right = itemDatabase.GetItemById(rightItemID);
        if(left != null)
            TryEquipItemToHand(CreateGridCell(left, leftQuantity), true);
        if(right != null)
            TryEquipItemToHand(CreateGridCell(right, rightQuantity), false);
        
        SetOpen(false);
    }

    private void OnToggleInventory(ToggleInventoryEvent e)
    {
        Debug.Log($"InventoryToggle - {currentGameState.ToString()}");
        
        // only toggleable in Play-Mode (& Inventory-Mode of course)
        if (currentGameState != GameStateChangedEvent.GameState.Play && currentGameState != GameStateChangedEvent.GameState.Inventory)
            return;
        
        Debug.Log("Toggle Inventory!");
        
        SetOpen(!isOpen);
        // isOpen is now set correctly, raise event for GameState
        GameEventManager.Raise(new InventoryVisibilityChangeEvent(isOpen));
    }

    private void BindDocument()
    {
        root = document.rootVisualElement;
        
        ApplyFontToAllTextElements(root, pirateFont);
        
        panel = root.Q<VisualElement>("inventory-panel");
        listScroller = root.Q<ScrollView>("inventory-scroll");
        list = root.Q<VisualElement>("inventory-grid");
        statusLabel = root.Q<Label>("status-label");
        
        dragGhost = root.Q<VisualElement>("inventory-slot-drag-ghost");
        
        details = root.Q<VisualElement>("inventory-details");
        
        itemDetails.Icon = root.Q<Image>("inventory-details-icon");
        itemDetails.Name = root.Q<Label>("inventory-details-name");
        itemDetails.Description = root.Q<Label>("inventory-details-description");
        itemDetails.Quantity = root.Q<Label>("inventory-details-quantity");
        itemDetails.DropButton = root.Q<Button>("drop-button");
        
        hands.parent = root.Q<VisualElement>("inventory-grid-hands");
        // left hand
        hands.leftHand.parent = hands.parent.Q<VisualElement>("left-hand-equip-slot");
        hands.leftHand.icon = hands.leftHand.parent.Q<Image>(className: "inventory-slot-icon");
        hands.leftHand.quantity = hands.leftHand.parent.Q<Label>(className: "inventory-slot-quantity");
        hands.leftHand.itemId = hands.leftHand.parent.Q<Label>(className: "inventory-slot-hidden-id");
        // right hand
        hands.rightHand.parent = hands.parent.Q<VisualElement>("right-hand-equip-slot");
        hands.rightHand.icon = hands.rightHand.parent.Q<Image>(className: "inventory-slot-icon");
        hands.rightHand.quantity = hands.rightHand.parent.Q<Label>(className: "inventory-slot-quantity");
        hands.rightHand.itemId = hands.rightHand.parent.Q<Label>(className: "inventory-slot-hidden-id");

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

        if (root != null)
        {
            root.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
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
            // no items in inventory (maybe show some text)
            
            listScroller.style.display = DisplayStyle.None;

            if (statusLabel != null)
            {
                statusLabel.text = "Capacity: 0/" + Inventory.Instance.inventoryCapacity;
            }

            return;
        }

        // items in inventory (maybe disable "no items"-text)

        listScroller.style.display = DisplayStyle.Flex;
        
        // generate Left and Right Hand Equip Cells
        //VisualElement[] equipCells = CreateEquipCells();
        //foreach(VisualElement equipCell in equipCells)
            //list.contentContainer.Add(equipCell);

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
            
            // is this item in one of my hands? -> do not draw
            if (itemData.ItemId == hands.leftHand.itemId.text || itemData.ItemId == hands.rightHand.itemId.text)
                continue;

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

        SetEventsForDragAndDrop(element, false);

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

    private void SetEventsForDragAndDrop(VisualElement element, bool forHands)
    {
        if (element == null)
            return;
        
        element.RegisterCallback<PointerDownEvent>(OnDragPointerDown);
        element.RegisterCallback<PointerMoveEvent>(OnDragPointerMove);
        element.RegisterCallback<PointerUpEvent>(OnDragPointerUp);
    }

    private void OnDragPointerDown(PointerDownEvent evt)
    {
        VisualElement element = evt.currentTarget as VisualElement;
        
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
    }

    private void OnDragPointerMove(PointerMoveEvent evt)
    {
        VisualElement element = evt.currentTarget as VisualElement;
        
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
    }

    private void OnDragPointerUp(PointerUpEvent evt)
    {
        VisualElement element = evt.currentTarget as VisualElement;
        
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
    }

    private void UnsetEventsForDragAndDrop(VisualElement element)
    {
        if (element == null)
            return;

        element.UnregisterCallback<PointerDownEvent>(OnDragPointerDown);
        element.UnregisterCallback<PointerMoveEvent>(OnDragPointerMove);
        element.UnregisterCallback<PointerUpEvent>(OnDragPointerUp);
    }

    private void StartDragging(VisualElement element, Vector2 position)
    {
        if (isDragging || pendingDragElement == null || dragGhost == null)
            return;
        
        isDragging = true;
        
        // Difference between mouse position and element's top-left corner.
        dragOffset = position - element.worldBound.position;  // use element (not dragGhost) to get correct offset

        if (element.ClassListContains("hand-equip"))
        {
            if (element.ClassListContains("left"))
            {
                hands.leftHand.quantity.visible = false;
                hands.leftHand.icon.visible = false;
            }
            else if (element.ClassListContains("right"))
            {
                hands.rightHand.quantity.visible = false;
                hands.rightHand.icon.visible = false;
            }
        }
        else
            element.style.display = DisplayStyle.None; // deactivate real element in list

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
            bool droppedOnInventoryGrid = list.worldBound.Contains(position);
            bool droppedOnLeftHand = hands.leftHand.parent.worldBound.Contains(position);
            bool droppedOnRightHand = hands.rightHand.parent.worldBound.Contains(position);
            
            if (element.ClassListContains("hand-equip"))  // recover hand equip
            {
                if (element.ClassListContains("left"))
                {
                    hands.leftHand.quantity.visible = true;
                    hands.leftHand.icon.visible = true;
                }
                else if (element.ClassListContains("right"))
                {
                    hands.rightHand.quantity.visible = true;
                    hands.rightHand.icon.visible = true;
                }
            }

            if (droppedOnInventoryGrid)  // reorder items
                TryReorderInsideInventory(element, position);
            else if (droppedOnLeftHand || droppedOnRightHand)  // equip item
                TryEquipItemToHand(element, droppedOnLeftHand);
        }
        else  // drop item
            DropItemIntoWorld(element, true);

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

        bool inLeftHand = itemID == hands.leftHand.itemId.text;
        bool inRightHand = itemID == hands.rightHand.itemId.text;
        if (inLeftHand || inRightHand) // remove current equipped item
        {
            GameEventManager.Raise(new PlayerStripItemEvent(inLeftHand));
            RemoveItemFromHand(inLeftHand, inRightHand);
        }

        InventoryItemDataSO itemData = itemDatabase.GetItemById(itemID);
        
        GameObject droppedItem = Instantiate(itemPrefab, player.DropOrigin.position, Quaternion.identity);

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
    
    private void TryReorderInsideInventory(VisualElement element, Vector2 pointerWorldPosition)
    {
        if (element == null || list == null)
            return;
        
        Label itemIdLabel = element.Q<Label>(className: "inventory-slot-hidden-id");
        Label quantityLabel = element.Q<Label>(className: "inventory-slot-quantity");
        if (itemIdLabel == null || quantityLabel == null)
            return;
        string itemID = itemIdLabel.text;
        int quantity = Int32.Parse(quantityLabel.text.Remove(0, 1));  // remove "x" from "x5"
        InventoryItemDataSO itemData = itemDatabase.GetItemById(itemID);
        
        bool inLeftHand = itemID == hands.leftHand.itemId.text;
        bool inRightHand = itemID == hands.rightHand.itemId.text;
        
        VisualElement container = list.contentContainer;

        int targetIndex = GetInventoryInsertIndex(container, element, pointerWorldPosition);
        
        if (inLeftHand || inRightHand) // remove current equipped item
        {
            // if item is currently in hand -> clear hand to nothing & create new grid cell for item
            GameEventManager.Raise(new PlayerStripItemEvent(inLeftHand));
            RemoveItemFromHand(inLeftHand, inRightHand);
            element = CreateGridCell(itemData, quantity);
        }
        
        // If the dragged element is already in this parent, remove it first
        // so the target index is applied cleanly.
        if (element.parent == container)
            element.RemoveFromHierarchy();

        targetIndex = Mathf.Clamp(targetIndex, 0, container.childCount);
        container.Insert(targetIndex, element);
        element.style.display = DisplayStyle.Flex;
    }
    
    private int GetInventoryInsertIndex(VisualElement container, VisualElement draggedElement, Vector2 pointerWorldPosition)
    {
        int index = 0;
        int lastIndexInPointerRow = -1;

        foreach (VisualElement child in container.Children())
        {
            if (child == draggedElement)
                continue;

            if (!child.ClassListContains("inventory-slot"))
                continue;

            Rect bounds = child.worldBound;

            float childCenterX = bounds.xMin + bounds.width * 0.5f;
            float childCenterY = bounds.yMin + bounds.height * 0.5f;

            bool pointerIsInSameRow =
                pointerWorldPosition.y >= bounds.yMin &&
                pointerWorldPosition.y <= bounds.yMax;

            if (pointerIsInSameRow)
            {
                lastIndexInPointerRow = index;

                if (pointerWorldPosition.x < childCenterX)
                    return index;
            }
            else if (lastIndexInPointerRow < 0 && pointerWorldPosition.y < childCenterY)
            {
                return index;
            }

            index++;
        }

        Debug.Log("Last");
        if (lastIndexInPointerRow >= 0)
            return lastIndexInPointerRow + 1;

        return index;
    }
    
    private void TryEquipItemToHand(VisualElement element, bool leftHand)
    {
        // remove current equipped item
        GameEventManager.Raise(new PlayerStripItemEvent(leftHand));
        
        // retrieve data from element
        Image iconImage =  element.Q<Image>(className: "inventory-slot-icon");
        Label itemIdLabel = element.Q<Label>(className: "inventory-slot-hidden-id");
        Label quantityLabel = element.Q<Label>(className: "inventory-slot-quantity");
        if (itemIdLabel == null || quantityLabel == null)
            return;
        int quantity = Int32.Parse(quantityLabel.text.Remove(0, 1));
        string itemID = itemIdLabel.text;
        
        if (leftHand)
        {
            hands.leftHand.icon.image = iconImage.image;
            hands.leftHand.quantity.text = $"x{quantity}";
            hands.leftHand.itemId.text = itemID;
            SetEventsForDragAndDrop(hands.leftHand.parent, true);
        }
        else
        {
            hands.rightHand.icon.image = iconImage.image;
            hands.rightHand.quantity.text = $"x{quantity}";
            hands.rightHand.itemId.text = itemID;
            SetEventsForDragAndDrop(hands.rightHand.parent, true);
        }
        EquipItemToPlayerHand(itemID, quantity, leftHand);
        
        // Remove old hand if hand was dropped on other hand
        if (element.ClassListContains("hand-equip"))  // element that is dragged is also hand
        {
            bool isLeft = element.ClassListContains("left");
            bool isRight = element.ClassListContains("right");
            if (isLeft && leftHand || isRight && !leftHand)  // hand dropped on itself
            {
                return;
            }
            // Remove hand A that was now dropped on hand B from original spot
            else if (!leftHand && isLeft || leftHand && isRight)
            {
                RemoveItemFromHand(!leftHand && isLeft, leftHand && isRight);
                GameEventManager.Raise(new PlayerStripItemEvent(isLeft));
            }
        }
        
        // update inventory render
        RenderInventory(lastInventory);
    }

    private void OnItemRemoveDueToUse(RemoveItemFromHandForUseEvent e)
    {
        bool leftHand = e.itemData.ItemId == hands.leftHand.itemId.text;
        bool rightHand = e.itemData.ItemId == hands.rightHand.itemId.text;
        
        int remaining = Inventory.Instance.Remove(e.itemData, 1);

        // remove item from hand without destroying it
        GameEventManager.Raise(new PlayerStripItemEvent(leftHand, true));
        
        if(remaining <= 0)
            RemoveItemFromHand(leftHand, rightHand);  // remove from UI
        else
        {
            UpdateHandEquipment(leftHand, remaining); // update amount in UI
            // generate new GameObject for Player Hand
            string leftItemID = hands.leftHand.itemId.text;
            string rightItemID = hands.rightHand.itemId.text;
            EquipItemToPlayerHand(leftHand ? leftItemID : rightItemID, remaining, leftHand);
        }

    }

    private void RemoveItemFromHand(bool leftHand, bool rightHand)
    {
        if (leftHand)
        {
            hands.leftHand.icon.image = null;
            hands.leftHand.quantity.text = "";
            hands.leftHand.itemId.text = "";
            UnsetEventsForDragAndDrop(hands.leftHand.parent);
        }
        if (rightHand)
        {
            hands.rightHand.icon.image = null;
            hands.rightHand.quantity.text = "";
            hands.rightHand.itemId.text = "";
            UnsetEventsForDragAndDrop(hands.rightHand.parent);
        }
        GameConfiguration.SaveHandEquipment(hands.leftHand.itemId.text, hands.rightHand.itemId.text);
    }
    
    private void UpdateHandEquipment(bool leftHand, int quantity)
    {
        if (leftHand)
            hands.leftHand.quantity.text = $"x{quantity}";
        else
            hands.rightHand.quantity.text = $"x{quantity}";
    }

    private void EquipItemToPlayerHand(string itemID, int quantity, bool leftHand)
    {
        InventoryItemDataSO itemData = itemDatabase.GetItemById(itemID);

        GameObject handEquip = leftHand ? player.LeftHandEquip : player.RightHandEquip;
        GameObject equippedItem = Instantiate(itemPrefab, handEquip.transform.position, Quaternion.identity);
        CollectableItem itemScript = equippedItem.GetComponentInChildren<CollectableItem>();
        itemScript.InventoryItemData = itemData;
        itemScript.amount = quantity;

        Collider[] cols = equippedItem.GetComponents<Collider>();
        if(cols != null && cols.Length > 0)
            foreach(Collider col in cols)
                col.enabled = false;
        
        Rigidbody rb = equippedItem.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;
        
        GameConfiguration.SaveHandEquipment(hands.leftHand.itemId.text, hands.rightHand.itemId.text);
        GameEventManager.Raise(new PlayerEquipItemEvent(equippedItem, leftHand));
    }
}
