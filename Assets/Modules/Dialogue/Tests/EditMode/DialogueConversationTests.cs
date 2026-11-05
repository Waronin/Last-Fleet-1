using System;
using LastFleet.Shared.Text;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Dialogue.Tests
{
    public class DialogueConversationTests
    {
        static readonly GameText Speaker = GameText.Verbatim("Speaker");

        [Test]
        public void Choice_RunsItsConsequenceAndMovesToItsNode()
        {
            int accepted = 0;
            DialogueScript script = new DialogueBuilder()
                .Say("ask", Speaker, Line("ask"))
                .Choice(Line("yes"), "thanks", () => accepted++)
                .Choice(Line("no"), null)
                .Say("thanks", Speaker, Line("thanks"))
                .Build();
            DialogueConversation conversation = new DialogueConversation(script);

            conversation.Choose(0);

            Assert.AreEqual(1, accepted);
            Assert.AreEqual("thanks", conversation.Current.Id);
        }

        [Test]
        public void ChoiceWithoutNextNode_EndsTheConversation()
        {
            DialogueScript script = new DialogueBuilder()
                .Say("ask", Speaker, Line("ask"))
                .Choice(Line("no"), null)
                .Build();
            DialogueConversation conversation = new DialogueConversation(script);

            conversation.Choose(0);

            Assert.IsTrue(conversation.IsFinished);
        }

        [Test]
        public void Continue_FollowsThenAndEndsAtTheLastNode()
        {
            DialogueScript script = new DialogueBuilder()
                .Say("first", Speaker, Line("first")).Then("second")
                .Say("second", Speaker, Line("second"))
                .Build();
            DialogueConversation conversation = new DialogueConversation(script);

            conversation.Continue();
            Assert.AreEqual("second", conversation.Current.Id);
            conversation.Continue();
            Assert.IsTrue(conversation.IsFinished);
        }

        [Test]
        public void Continue_IsRefusedWhileAChoiceIsOpen()
        {
            DialogueConversation conversation = new DialogueConversation(new DialogueBuilder()
                .Say("ask", Speaker, Line("ask")).Choice(Line("ok"), null).Build());

            Assert.Throws<InvalidOperationException>(() => conversation.Continue());
        }

        [Test]
        public void Script_RejectsUnknownAndDuplicateNodes()
        {
            Assert.Throws<ArgumentException>(() => new DialogueBuilder()
                .Say("ask", Speaker, Line("ask")).Choice(Line("go"), "missing").Build());
            Assert.Throws<ArgumentException>(() => new DialogueBuilder()
                .Say("same", Speaker, Line("a"))
                .Say("same", Speaker, Line("b"))
                .Build());
        }

        [Test]
        public void Service_ShowsQueuedDialoguesOneAfterAnother()
        {
            GameObject host = new GameObject("Dialogue Test");
            try
            {
                DialogueService service = host.AddComponent<DialogueService>();
                service.Open(DialogueBuilder.Message(Speaker, Line("first")));
                service.Open(DialogueBuilder.Message(Speaker, Line("second")));

                Assert.AreEqual(1, service.WaitingCount);
                Assert.AreEqual("first", service.Current.Current.Line.VerbatimText);
                service.Continue();
                Assert.AreEqual("second", service.Current.Current.Line.VerbatimText);
                service.Continue();
                Assert.IsNull(service.Current);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        static GameText Line(string text)
        {
            return GameText.Verbatim(text);
        }
    }
}
