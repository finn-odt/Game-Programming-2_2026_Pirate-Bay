using System;
using System.Collections.Generic;
using GameEvents;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using EngineCursor = UnityEngine.Cursor;
using EngineCursorLockMode = UnityEngine.CursorLockMode;

[RequireComponent(typeof(UIDocument))]
public class ConversationUI : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    
    [SerializeField] private Font pirateFont;

    private static VisualElement root;
    private static VisualElement panel;
    private static Label spokenWords;
    
    private static bool isOpen;

    private static GameStateChangedEvent.GameState currentGameState = GameStateChangedEvent.GameState.Intro;
    
    private readonly InputAction acceptOfferAction = new("Accept Conversation Offer", InputActionType.Button);
    private readonly InputAction declineOfferAction = new("Decline Conversation Offer", InputActionType.Button);

    private void Awake()
    {
        if (document == null)
        {
            document = GetComponent<UIDocument>();
        }

        acceptOfferAction.AddBinding("<Keyboard>/enter");
        acceptOfferAction.AddBinding("<Gamepad>/buttonSouth");  // A
        
        declineOfferAction.AddBinding("<Keyboard>/backspace");
        declineOfferAction.AddBinding("<Gamepad>/buttonEast");  // B
    }

    private void OnEnable()
    {
        GameEventManager.AddListener<ConversationUIEvent>(OnConversationRequest);
        GameEventManager.AddListener<GameStateChangedEvent>(OnGameStateChange);
        
        acceptOfferAction.Enable();
        acceptOfferAction.performed += OnAcceptance;
        
        declineOfferAction.Enable();
        declineOfferAction.performed += OnRejection;
    }

    private void OnDisable()
    {
        GameEventManager.RemoveListener<ConversationUIEvent>(OnConversationRequest);
        
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
        
        acceptOfferAction.performed -= OnAcceptance;
        acceptOfferAction.Disable();
        
        declineOfferAction.performed -= OnRejection;
        declineOfferAction.Disable();
    }

    private void OnAcceptance(InputAction.CallbackContext context)
    {
        if (isOpen && currentGameState == GameStateChangedEvent.GameState.Play)
        {
            SetOpen(false, "");  // close conversation
            GameEventManager.Raise(new ConversationAnswerEvent(true));
        }
    }

    private void OnRejection(InputAction.CallbackContext context)
    {
        if (isOpen && currentGameState == GameStateChangedEvent.GameState.Play)
        {
            SetOpen(false, "");  // close conversation
            GameEventManager.Raise(new ConversationAnswerEvent(false));
        }
    }
    
    private void OnConversationRequest(ConversationUIEvent e)
    {
        SetOpen(e.isOpen, e.message);
    }

    private void OnGameStateChange(GameStateChangedEvent gameEvent)
    {
        currentGameState = gameEvent.newState;
    }

    private void Start()
    {
        BindDocument();
        SetOpen(false, "");
    }

    private void BindDocument()
    {
        root = document.rootVisualElement;
        
        panel = root.Q<VisualElement>("conversation-screen");
        spokenWords = root.Q<Label>("spoken-words");

        ApplyFontToAllTextElements(root, pirateFont);
    }
    
    private void ApplyFontToAllTextElements(VisualElement root, Font font)
    {
        // Query all elements that derive from TextElement (Label, Button text, etc.)
        foreach (var textElement in root.Query<TextElement>().ToList())
        {
            textElement.style.unityFont = font;
        }
    }

    private void SetOpen(bool open, string message)
    {
        isOpen = open;
        
        if (root != null)
        {
            root.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (panel != null)
        {
            panel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        if (isOpen)
        {
            RenderConversation(message);
        }
    }

    private void RenderConversation(string message)
    {
        if (spokenWords == null)
        {
            return;
        }

        spokenWords.text = message;
    }
}
