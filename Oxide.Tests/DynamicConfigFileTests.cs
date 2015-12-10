﻿using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Tests
{
    [TestClass]
    public class DynamicConfigFileTests
    {
        [TestMethod]
        public void WhenYouGetNotExistingValueTypeSetting_ThenDefaultInstanceIsCreated()
        {
            // Given
            var filename = Path.Combine(Interface.Oxide.ConfigDirectory, Path.GetRandomFileName());
            File.WriteAllText(filename, "{}");
            var configFile = ConfigFile.Load<DynamicConfigFile>(filename);

            // When
            var value = configFile.Get<int>("NotExistingSettingKey");

            // Then
            Assert.AreEqual(value, default(int));
        }

        [TestMethod]
        public void WhenYouGetNotExistingRefTypeSetting_ThenNullShouldBeReturned()
        {
            // Given
            var filename = Path.Combine(Interface.Oxide.ConfigDirectory, Path.GetRandomFileName());
            File.WriteAllText(filename, "{}");
            var configFile = ConfigFile.Load<DynamicConfigFile>(filename);

            // When
            var value = configFile.Get<StringBuilder>("NotExistingSettingKey");

            // Then
            Assert.IsNull(value);
        }

        public class ConfigTestObject
        {
            public int a { get; set; }
        }

        [TestMethod]
        public void WhenYouGetExistingValueSettingWithoutAnyFieldSpecified_ThenDefaultObjectShouldBeReturned()
        {
            // Given
            var filename = Path.Combine(Interface.Oxide.ConfigDirectory, Path.GetRandomFileName());
            File.WriteAllText(filename, "{ \"a\": { \"a\":\"12\"} }");
            var configFile = ConfigFile.Load<DynamicConfigFile>(filename);
            var expectedValue = new ConfigTestObject() { a = 12 };

            // When
            var value = configFile.Get<ConfigTestObject>("a");

            // Then
            Assert.IsInstanceOfType(value, typeof(ConfigTestObject));
            Assert.AreEqual(value.a, 12);
        }

        [TestMethod]
        public void WhenYouGetSettingValueOfType_ThenInvalidCastExceptionShouldBeRaised()
        {
            // Given
            var filename = Path.Combine(Interface.Oxide.ConfigDirectory, Path.GetRandomFileName());
            File.WriteAllText(filename, "{ \"a\": { \"bc\":\"12\"} }");
            var configFile = ConfigFile.Load<DynamicConfigFile>(filename);

            // When
            var value = configFile.Get<ConfigTestObject>("a");

            // Then
            Assert.IsInstanceOfType(value, typeof(ConfigTestObject));
            Assert.AreNotEqual(value.a, 12);
        }

        [TestMethod]
        public void WhenYouGetExistingValueSettingThatIsConvertibleToTargetType_ThenExpectedValueOfCorrectTypeShouldBeReturned()
        {
            // Given
            var filename = Path.Combine(Interface.Oxide.ConfigDirectory, Path.GetRandomFileName());
            File.WriteAllText(filename, "{ \"a\":\"12\" }");
            var configFile = ConfigFile.Load<DynamicConfigFile>(filename);

            // When
            var value = configFile.Get<float>("a");

            // Then
            Assert.AreEqual(value, 12.0f);
        }

        [TestMethod]
        public void WhenYouGetExistingSettingThatIsNotConvertibleToTargetType_ThenDefaultValueShouldBeReturned()
        {
            // Given
            var filename = Path.Combine(Interface.Oxide.ConfigDirectory, Path.GetRandomFileName());
            File.WriteAllText(filename, "{ \"a\":\"12\", \"b\" : { } }");
            var configFile = ConfigFile.Load<DynamicConfigFile>(filename);

            // When
            var valTypeSettingValue = configFile.Get<StringBuilder>("a");
            var refTypeSettingValue = configFile.Get<int>("b");

            // Then
            Assert.AreEqual(valTypeSettingValue, null);
            Assert.AreEqual(refTypeSettingValue, default(int));
        }

        [TestMethod]
        public void DynamicConfigLoadSaveTest()
        {
            const string inputfile = "{ \"x\": 10, \"y\": \"hello\", \"z\": [ 10, \"yo\" ], \"w\": { \"a\": 20, \"b\": [ 500, 600 ] } }";
            var filename = Path.Combine(Interface.Oxide.ConfigDirectory, Path.GetRandomFileName());
            File.WriteAllText(filename, inputfile);

            var cfg = new DynamicConfigFile(filename);
            cfg.Load();

            TestConfigFile(cfg);

            cfg.Save(filename);
            cfg = ConfigFile.Load<DynamicConfigFile>(filename);

            TestConfigFile(cfg);

            File.Delete(filename);
        }

        private void TestConfigFile(DynamicConfigFile cfg)
        {
            Assert.AreEqual(10, cfg["x"], "Failed cfg.x");
            Assert.AreEqual("hello", cfg["y"], "Failed cfg.y");

            var list = cfg["z"] as List<object>;
            Assert.AreNotEqual(null, list, "Failed cfg.z");
            if (list != null)
            {
                Assert.AreEqual(2, list.Count, "Failed cfg.z.Count");
                if (list.Count == 2)
                {
                    Assert.AreEqual(10, list[0], "Failed cfg.z[0]");
                    Assert.AreEqual("yo", list[1], "Failed cfg.z[1]");
                }
            }

            var dict = cfg["w"] as Dictionary<string, object>;
            Assert.AreNotEqual(null, dict, "Failed cfg.w");
            if (dict == null) return;
            Assert.AreEqual(2, dict.Count, "Failed cfg.w.Count");
            if (dict.Count != 2) return;
            object tmp;
            Assert.AreEqual(true, dict.TryGetValue("a", out tmp), "Failed cfg.w.a");
            Assert.AreEqual(20, tmp, "Failed cfg.w.a");
            Assert.AreEqual(true, dict.TryGetValue("b", out tmp), "Failed cfg.w.b");

            list = tmp as List<object>;
            Assert.AreNotEqual(null, list, "Failed cfg.w.b");
            if (list == null) return;
            Assert.AreEqual(2, list.Count, "Failed cfg.w.b.Count");
            if (list.Count != 2) return;
            Assert.AreEqual(500, list[0], "Failed cfg.w.b[0]");
            Assert.AreEqual(600, list[1], "Failed cfg.w.b[1]");
        }
    }
}