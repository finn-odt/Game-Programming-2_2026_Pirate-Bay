using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEvents
{
    public static class GameEventManager
    {
        // public API delegate (generic)
        public delegate void EventDelegate<in T>(T e) where T : GameEvent;
        // private delegate for saving all events in one Dictionary
		private delegate void EventDelegate(GameEvent e);

        private static readonly Dictionary<System.Type, EventDelegate> _delegates;  // System.Type t = typeof(PlayerDiedEvent);
		private static readonly Dictionary<System.Delegate, EventDelegate> _delegateLookup;  // any delegate can be used as key
		private static readonly Dictionary<(Type eventType, Delegate listener), EventDelegate> _runtimeDelegateLookup;  // delegates that's type is instantiated on runtime

        static GameEventManager() {
			_delegates = new Dictionary<System.Type, EventDelegate>();
			_delegateLookup = new Dictionary<System.Delegate, EventDelegate>();
			_runtimeDelegateLookup = new Dictionary<(Type eventType, Delegate listener), EventDelegate>();
		}
        
		/// <summary>
		/// Compile-Time Generic Listener Registration
		/// </summary>
        public static void AddListener<T>(EventDelegate<T> del) where T : GameEvent {
			// Early-out if we've already registered this delegate
			if (_delegateLookup.ContainsKey(del))
				return;

			// Create a new non-generic delegate which calls our generic one.
			// This is the delegate we actually invoke.
			void InternalDelegate(GameEvent e) => del((T) e);  // lambda function
			_delegateLookup[del] = InternalDelegate;

			EventDelegate tempDel;
			if (_delegates.TryGetValue(typeof(T), out tempDel)) {
                // add new delegate to existing ones
				_delegates[typeof(T)] = tempDel += InternalDelegate;
			}
			else {
                // add new delegate (no existing ones yet)
				_delegates[typeof(T)] = InternalDelegate;
			}
		}
        
		/// <summary>
		/// Runtime-Time Generic Listener Registration
		/// </summary>
		public static void AddListener(Type eventType, Action<GameEvent> listener)
		{
			if (eventType == null)
				throw new ArgumentNullException(nameof(eventType));

			if (listener == null)
				throw new ArgumentNullException(nameof(listener));

			if (!typeof(GameEvent).IsAssignableFrom(eventType))
				throw new ArgumentException($"{eventType.FullName} is not a GameEvent type.", nameof(eventType));

			var key = (eventType, (Delegate)listener);

			if (_runtimeDelegateLookup.ContainsKey(key))
				return;

			void InternalDelegate(GameEvent e) => listener(e);

			_runtimeDelegateLookup[key] = InternalDelegate;

			if (_delegates.TryGetValue(eventType, out EventDelegate existingDelegate))
				_delegates[eventType] = existingDelegate + InternalDelegate;
			else
				_delegates[eventType] = InternalDelegate;
		}
		
		/// <summary>
		/// Compile-Time Generic Listener De-Registration
		/// </summary>
        public static void RemoveListener<T>(EventDelegate<T> del) where T : GameEvent {
			EventDelegate internalDelegate;  // get interal GameEvent-Delegate
			if (_delegateLookup.TryGetValue(del, out internalDelegate)) {
				EventDelegate tempDel;  // get external derived Delegate
				if (_delegates.TryGetValue(typeof(T), out tempDel)) {
					tempDel -= internalDelegate;  // remove delegate from existing
					if (tempDel == null) {
						_delegates.Remove(typeof(T));
					}
					else {
						_delegates[typeof(T)] = tempDel;
					}
				}

				_delegateLookup.Remove(del);
			}
		}

        /// <summary>
        /// Runtime-Time Generic Listener De-Registration
        /// </summary>
        public static void RemoveListener(Type eventType, Action<GameEvent> listener)
        {
	        if (eventType == null || listener == null)
		        return;

	        var key = (eventType, (Delegate)listener);

	        if (!_runtimeDelegateLookup.TryGetValue(key, out EventDelegate internalDelegate))
		        return;

	        if (_delegates.TryGetValue(eventType, out EventDelegate existingDelegate))
	        {
		        existingDelegate -= internalDelegate;

		        if (existingDelegate == null)
			        _delegates.Remove(eventType);
		        else
			        _delegates[eventType] = existingDelegate;
	        }

	        _runtimeDelegateLookup.Remove(key);
        }
        
        public static void Raise(GameEvent e) {
			EventDelegate del;  // get all delegates of this type
			if (_delegates.TryGetValue(e.GetType(), out del)) {
				del.Invoke(e);  // raise event
			}
		}

		public static void Clear() {
		    _delegates.Clear();
		    _delegateLookup.Clear();
		    _runtimeDelegateLookup.Clear();
		}

		public static void Clear<T>() where T : GameEvent {
		    System.Type type = typeof(T);

		    if (!_delegates.TryGetValue(type, out EventDelegate multicastDelegate)) {
		        return;
		    }

		    foreach (System.Delegate internalDelegate in multicastDelegate.GetInvocationList()) {
		        System.Delegate externalDelegateToRemove = null;

		        foreach (KeyValuePair<System.Delegate, EventDelegate> pair in _delegateLookup) {
		            if (pair.Value == (EventDelegate)internalDelegate) {
		                externalDelegateToRemove = pair.Key;
		                break;
		            }
		        }

		        if (externalDelegateToRemove != null) {
		            _delegateLookup.Remove(externalDelegateToRemove);
		        }
		    }

		    _delegates.Remove(type);
		}

		public static void Clear(GameEvent e) {
		    if (e == null) {
		        Clear();
		        return;
		    }

		    Clear(e.GetType());
		}

		public static void Clear(System.Type eventType) {
		    if (eventType == null) {
		        Clear();
		        return;
		    }

		    if (!_delegates.TryGetValue(eventType, out EventDelegate multicastDelegate)) {
		        return;
		    }

		    foreach (System.Delegate internalDelegate in multicastDelegate.GetInvocationList()) {
		        System.Delegate externalDelegateToRemove = null;

		        foreach (KeyValuePair<System.Delegate, EventDelegate> pair in _delegateLookup) {
		            if (pair.Value == (EventDelegate)internalDelegate) {
		                externalDelegateToRemove = pair.Key;
		                break;
		            }
		        }

		        if (externalDelegateToRemove != null) {
		            _delegateLookup.Remove(externalDelegateToRemove);
		        }
		    }

		    _delegates.Remove(eventType);
		}
    }
}
