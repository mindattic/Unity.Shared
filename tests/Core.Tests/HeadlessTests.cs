using System;
using NUnit.Framework;

namespace Unity.Shared.Core.Tests
{
    [NonParallelizable]
    public class HeadlessTests
    {
        string savedEnv;

        [SetUp]
        public void SetUp()
        {
            savedEnv = Environment.GetEnvironmentVariable(Headless.EnvironmentVariable);
            Environment.SetEnvironmentVariable(Headless.EnvironmentVariable, null);
            Headless.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(Headless.EnvironmentVariable, savedEnv);
            Headless.Reset();
        }

        [TestCase("--headless")]
        [TestCase("-headless")]
        [TestCase("--HEADLESS")]
        [TestCase("-Headless")]
        public void EitherSwitchSpellingInAnyCaseTurnsItOn(string arg)
        {
            Assert.That(Headless.Requested(new[] { "Game.exe", "-batchmode", arg, "-nographics" }, null), Is.True);
        }

        [TestCase("--headless=1")]
        [TestCase("headless")]
        [TestCase("---headless")]
        [TestCase("-headlessly")]
        [TestCase("-muted")]
        public void OtherArgumentsDoNot(string arg)
        {
            Assert.That(Headless.Requested(new[] { "Game.exe", arg }, null), Is.False);
        }

        [TestCase("1", true)]
        [TestCase(" true ", true)]
        [TestCase("YES", true)]
        [TestCase("on", true)]
        [TestCase("0", false)]
        [TestCase("false", false)]
        [TestCase("", false)]
        [TestCase("  ", false)]
        [TestCase(null, false)]
        public void TheEnvironmentValueIsRead(string value, bool expected)
        {
            Assert.That(Headless.Requested(new[] { "Game.exe" }, value), Is.EqualTo(expected));
        }

        [Test]
        public void NullArgumentsAskForNothing()
        {
            Assert.That(Headless.Requested(null, null), Is.False);
            Assert.That(Headless.Requested(null, "1"), Is.True);
        }

        [Test]
        public void OffByDefault()
        {
            // The test host has no --headless on its command line and the variable is cleared in SetUp.
            Assert.That(Headless.IsActive, Is.False);
        }

        [Test]
        public void TheEnvironmentVariableActivatesTheProcess()
        {
            Environment.SetEnvironmentVariable(Headless.EnvironmentVariable, "1");
            Assert.That(Headless.Requested(), Is.True);
            Assert.That(Headless.IsActive, Is.True);
        }

        [Test]
        public void ItIsResolvedOnce()
        {
            Assert.That(Headless.IsActive, Is.False);
            Environment.SetEnvironmentVariable(Headless.EnvironmentVariable, "1");
            Assert.That(Headless.IsActive, Is.False, "a later environment change does not flip a resolved session");
            Headless.Reset();
            Assert.That(Headless.IsActive, Is.True);
        }

        [Test]
        public void CodeCanOverrideItBothWays()
        {
            Headless.IsActive = true;
            Assert.That(Headless.IsActive, Is.True);
            Environment.SetEnvironmentVariable(Headless.EnvironmentVariable, "1");
            Headless.IsActive = false;
            Assert.That(Headless.IsActive, Is.False, "the override wins over the environment");
        }

        [Test]
        public void ActivatedFiresOnceWhenItTurnsOn()
        {
            int fired = 0;
            Headless.Activated += () => fired++;
            Assert.That(Headless.IsActive, Is.False);
            Headless.IsActive = true;
            Headless.IsActive = true;
            Assert.That(fired, Is.EqualTo(1));
            Headless.IsActive = false;
            Headless.IsActive = true;
            Assert.That(fired, Is.EqualTo(2), "off then on again is a new activation");
        }

        [Test]
        public void ActivatedFiresWhenTheFirstReadResolvesTrue()
        {
            int fired = 0;
            Headless.Activated += () => fired++;
            Environment.SetEnvironmentVariable(Headless.EnvironmentVariable, "1");
            Assert.That(Headless.IsActive, Is.True);
            Assert.That(Headless.IsActive, Is.True);
            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void WhenActiveRunsNowIfAlreadyHeadless()
        {
            Headless.IsActive = true;
            int ran = 0;
            Headless.WhenActive(() => ran++);
            Assert.That(ran, Is.EqualTo(1));
            Headless.IsActive = false;
            Headless.IsActive = true;
            Assert.That(ran, Is.EqualTo(1), "a WhenActive callback runs once");
        }

        [Test]
        public void WhenActiveWaitsForALaterActivation()
        {
            int ran = 0;
            Headless.WhenActive(() => ran++);
            Assert.That(ran, Is.EqualTo(0), "off by default: nothing runs");
            Headless.IsActive = true;
            Assert.That(ran, Is.EqualTo(1));
            Headless.IsActive = false;
            Headless.IsActive = true;
            Assert.That(ran, Is.EqualTo(1));
        }

        [Test]
        public void WhenActiveRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => Headless.WhenActive(null));
        }

        [Test]
        public void TheReadyLineStartsWithTheMarker()
        {
            Assert.That(Headless.ReadyLine("Unity.Eve"), Is.EqualTo("MINDATTIC_HEADLESS_READY Unity.Eve"));
            Assert.That(Headless.ReadyLine("  Grid Game "), Is.EqualTo("MINDATTIC_HEADLESS_READY Grid Game"));
            Assert.That(Headless.ReadyLine(null), Is.EqualTo("MINDATTIC_HEADLESS_READY"));
        }
    }
}
