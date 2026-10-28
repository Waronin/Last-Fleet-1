using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using NUnit.Framework;

namespace LastFleet.Shared.Tests
{
    public class GameTextTests
    {
        enum Sample
        {
            First,
            Second
        }

        [Test]
        public void Format_KeepsKeyAndNamedArguments()
        {
            GameTextKey key = new GameTextKey("Table", "entry");

            GameText text = key.Format(("ship", "Harvester 1"), ("count", 3));

            Assert.AreEqual(key, text.Key);
            Assert.IsFalse(text.IsVerbatim);
            Assert.AreEqual(2, text.Arguments.Count);
            Assert.AreEqual("ship", text.Arguments[0].Name);
            Assert.AreEqual("Harvester 1", text.Arguments[0].Value);
            Assert.AreEqual(3, text.Arguments[1].Value);
        }

        [Test]
        public void KeyWithoutArguments_ConvertsToText()
        {
            GameText text = new GameTextKey("Table", "entry");

            Assert.AreEqual("entry", text.Key.Entry);
            Assert.IsEmpty(text.Arguments);
        }

        [Test]
        public void Verbatim_SkipsTheKey()
        {
            GameText text = GameText.Verbatim("Harvester 3");

            Assert.IsTrue(text.IsVerbatim);
            Assert.AreEqual("Harvester 3", text.VerbatimText);
        }

        [Test]
        public void EnumTextKeys_HasOneEntryPerValue()
        {
            EnumTextKeys<Sample> names = new EnumTextKeys<Sample>("Table", "sample");

            List<GameTextKey> keys = new List<GameTextKey>(names.Keys);

            Assert.AreEqual(2, keys.Count);
            Assert.AreEqual("sample.Second", names.For(Sample.Second).Key.Entry);
            Assert.Throws<ArgumentOutOfRangeException>(() => names.For((Sample)7));
        }

        [Test]
        public void InvalidKeys_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new GameTextKey("", "entry"));
            Assert.Throws<ArgumentException>(() => new GameTextKey("Table", " "));
            Assert.Throws<ArgumentException>(() => new GameTextArgument("", 1));
        }
    }
}
