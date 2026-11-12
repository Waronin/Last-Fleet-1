using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Ships.Tests
{
    public class WreckScatterTests
    {
        [Test]
        public void OnePiece_StaysWhereTheShipWas()
        {
            List<Vector2> offsets = new List<Vector2>();

            WreckScatter.Offsets(1, 6f, 30f, offsets);

            Assert.AreEqual(1, offsets.Count);
            Assert.AreEqual(Vector2.zero, offsets[0]);
        }

        [Test]
        public void SeveralPieces_SpreadWithinTheRadiusAndKeepRoomBetweenThem()
        {
            List<Vector2> offsets = new List<Vector2>();

            WreckScatter.Offsets(6, 6f, 0f, offsets);

            Assert.AreEqual(6, offsets.Count);
            foreach (Vector2 offset in offsets)
            {
                Assert.LessOrEqual(offset.magnitude, 6f + 1e-4f);
                Assert.Greater(offset.magnitude, 0f, "No piece sits in the middle of the others.");
            }
            for (int i = 0; i < offsets.Count; i++)
            {
                for (int j = i + 1; j < offsets.Count; j++)
                {
                    // Two wreck radii (0.8 each) plus a gap: the pieces never overlap.
                    Assert.Greater((offsets[i] - offsets[j]).magnitude, 2f, $"Pieces {i} and {j} are too close.");
                }
            }
        }

        [Test]
        public void CapitalShipSections_KeepTheirSalvageSlotsApart()
        {
            // The refinery has the smallest hull: radius 4.5 plus 4.5 spread, sections of radius 2.5.
            List<Vector2> offsets = new List<Vector2>();
            float footprint = 2.5f + LastFleet.Zones.ResourceDeposit.SlotClearanceUnits;

            WreckScatter.Offsets(6, 9f, 0f, offsets);

            for (int i = 0; i < offsets.Count; i++)
            {
                for (int j = i + 1; j < offsets.Count; j++)
                {
                    Assert.GreaterOrEqual((offsets[i] - offsets[j]).magnitude, 2f * footprint, $"Sections {i} and {j} overlap.");
                }
            }
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            List<Vector2> offsets = new List<Vector2>();
            Assert.Throws<ArgumentOutOfRangeException>(() => WreckScatter.Offsets(0, 6f, 0f, offsets));
            Assert.Throws<ArgumentOutOfRangeException>(() => WreckScatter.Offsets(2, -1f, 0f, offsets));
        }
    }
}
