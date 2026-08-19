using System;
using GameEvents;
using UnityEngine;

[Serializable]
public class GameEventTypeReference
{
    [SerializeField] private string assemblyQualifiedTypeName;

    public string AssemblyQualifiedTypeName => assemblyQualifiedTypeName;

    public Type Type
    {
        get
        {
            if (string.IsNullOrEmpty(assemblyQualifiedTypeName))
                return null;

            Type type = System.Type.GetType(assemblyQualifiedTypeName);
            return IsRaiseableEventType(type) ? type : null;
        }
    }

    public bool HasValue => Type != null;

    public void Set(Type type)
    {
        if (type == null)
        {
            assemblyQualifiedTypeName = string.Empty;
            return;
        }
        
        if (!IsRaiseableEventType(type))
        {
            throw new ArgumentException(
                $"{type.FullName} must be a concrete, non-generic GameEvent type with a public parameterless constructor.",
                nameof(type));
        }

        assemblyQualifiedTypeName = type.AssemblyQualifiedName;
    }
    
    public GameEvent Create()
    {
        Type type = Type;

        if (type == null)
        {
            throw new InvalidOperationException("No raiseable GameEvent type is selected.");
        }

        return (GameEvent)Activator.CreateInstance(type);
    }

    public bool TryCreate(out GameEvent gameEvent)
    {
        gameEvent = null;
        Type type = Type;

        if (type == null)
            return false;

        gameEvent = (GameEvent)Activator.CreateInstance(type);
        return gameEvent != null;
    }

    public void Raise()
    {
        GameEvent gameEvent = Create();

        if (!gameEvent.isValid())
        {
            throw new InvalidOperationException($"{gameEvent.GetType().FullName} created an invalid event.");
        }

        GameEventManager.Raise(gameEvent);
    }

    public bool TryRaise()
    {
        if (!TryCreate(out GameEvent gameEvent))
            return false;

        if (!gameEvent.isValid())
            return false;

        GameEventManager.Raise(gameEvent);
        return true;
    }

    public static bool IsRaiseableEventType(Type type)
    {
        return type != null
               && type != typeof(GameEvent)
               && typeof(GameEvent).IsAssignableFrom(type)
               && type.IsClass
               && !type.IsAbstract
               && !type.IsGenericType
               && !type.ContainsGenericParameters
               && type.GetConstructor(Type.EmptyTypes) != null;
    }
}