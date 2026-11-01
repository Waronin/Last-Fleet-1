using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Ships.Tests
{
    public class ObstacleAvoidanceTests
    {
        const float LookAhead = 50f;
        const float Clearance = 2f;

        [Test]
        public void Steer_KeepsTheCourse_WhenNothingIsInTheWay()
        {
            List<Obstacle> obstacles = new List<Obstacle> { new Obstacle(new Vector2(20f, 20f), 3f, "rock") };

            Vector2 steer = ObstacleAvoidance.Steer(Vector2.zero, Vector2.up, new Vector2(0f, 100f), LookAhead, Clearance, obstacles, null);

            Assert.AreEqual(Vector2.up, steer);
        }

        [Test]
        public void Steer_PassesOnTheSideThatTheCourseLeansTo()
        {
            List<Obstacle> rightOfCourse = new List<Obstacle> { new Obstacle(new Vector2(1f, 20f), 4f, "rock") };
            List<Obstacle> leftOfCourse = new List<Obstacle> { new Obstacle(new Vector2(-1f, 20f), 4f, "rock") };

            Vector2 aroundLeft = ObstacleAvoidance.Steer(Vector2.zero, Vector2.up, new Vector2(0f, 100f), LookAhead, Clearance, rightOfCourse, null);
            Vector2 aroundRight = ObstacleAvoidance.Steer(Vector2.zero, Vector2.up, new Vector2(0f, 100f), LookAhead, Clearance, leftOfCourse, null);

            Assert.Less(aroundLeft.x, 0f);
            Assert.Greater(aroundRight.x, 0f);
            Assert.Greater(aroundLeft.y, 0f, "still heading forward");
        }

        [Test]
        public void Steer_IgnoresTheObstacleThatHoldsTheDestination_AndTheShipItself()
        {
            Vector2 destination = new Vector2(0f, 21f);
            List<Obstacle> obstacles = new List<Obstacle>
            {
                new Obstacle(new Vector2(0f, 20f), 3f, "mining target"),
                new Obstacle(new Vector2(0f, 5f), 3f, "self")
            };

            Vector2 steer = ObstacleAvoidance.Steer(Vector2.zero, Vector2.up, destination, LookAhead, Clearance, obstacles, "self");

            Assert.AreEqual(Vector2.up, steer);
        }

        [Test]
        public void Steer_DoesNotLookPastTheDestination()
        {
            List<Obstacle> obstacles = new List<Obstacle> { new Obstacle(new Vector2(0f, 40f), 3f, "rock") };

            Vector2 steer = ObstacleAvoidance.Steer(Vector2.zero, Vector2.up, new Vector2(0f, 20f), LookAhead, Clearance, obstacles, null);

            Assert.AreEqual(Vector2.up, steer);
        }

        [Test]
        public void IsLineBlocked_SeesCover_ButNotTheShooterOrTheTarget()
        {
            List<Obstacle> obstacles = new List<Obstacle>
            {
                new Obstacle(new Vector2(0f, 50f), 5f, "rock"),
                new Obstacle(new Vector2(0f, 100f), 8f, "target carrier")
            };

            Assert.IsTrue(ObstacleAvoidance.IsLineBlocked(Vector2.zero, new Vector2(0f, 100f), obstacles, "fighter", "target carrier"));
            Assert.IsFalse(ObstacleAvoidance.IsLineBlocked(Vector2.zero, new Vector2(20f, 100f), obstacles, "fighter", "target carrier"));
            Assert.IsFalse(ObstacleAvoidance.IsLineBlocked(Vector2.zero, new Vector2(0f, 40f), obstacles, "fighter", null));
        }
    }
}
