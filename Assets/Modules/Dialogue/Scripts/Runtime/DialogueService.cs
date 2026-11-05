using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastFleet.Dialogue
{
    /// <summary>
    /// Holds the conversation shown to the player and the ones waiting behind it, like radio calls
    /// coming in one after another. The game keeps running while a conversation is open. Who opens
    /// a conversation decides its consequences through the choices; the window only shows it.
    /// </summary>
    public sealed class DialogueService : MonoBehaviour
    {
        readonly Queue<DialogueScript> _waiting = new Queue<DialogueScript>();

        /// <summary>The conversation shown now, or null.</summary>
        public DialogueConversation Current { get; private set; }

        public int WaitingCount => _waiting.Count;

        /// <summary>Shows the dialogue now, or after the ones already open or waiting.</summary>
        public void Open(DialogueScript script)
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }
            _waiting.Enqueue(script);
            if (Current == null)
            {
                ShowNext();
            }
        }

        public void Continue()
        {
            if (Current == null)
            {
                return;
            }
            Current.Continue();
            ShowNextIfFinished();
        }

        public void Choose(int choiceIndex)
        {
            if (Current == null)
            {
                return;
            }
            Current.Choose(choiceIndex);
            ShowNextIfFinished();
        }

        /// <summary>Drops every conversation, e.g. when a new run starts.</summary>
        public void Clear()
        {
            _waiting.Clear();
            Current?.Abort();
            Current = null;
        }

        void ShowNextIfFinished()
        {
            if (Current != null && Current.IsFinished)
            {
                ShowNext();
            }
        }

        void ShowNext()
        {
            Current = _waiting.Count > 0 ? new DialogueConversation(_waiting.Dequeue()) : null;
        }
    }
}
