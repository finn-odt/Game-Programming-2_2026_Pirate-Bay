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

        static GameEventManager() {
			_delegates = new Dictionary<System.Type, EventDelegate>();
			_delegateLookup = new Dictionary<System.Delegate, EventDelegate>();
		}

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
        
        public static void Raise(GameEvent e) {
			EventDelegate del;  // get all delegates of this type
			if (_delegates.TryGetValue(e.GetType(), out del)) {
				del.Invoke(e);  // raise event
			}
		}
    }
}
