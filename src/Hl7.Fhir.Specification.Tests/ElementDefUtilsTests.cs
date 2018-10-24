﻿/* 
 * Copyright (c) 2018, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Hl7.Fhir.Model.ElementDefinitionUtilities;

namespace Hl7.Fhir.Specification.Tests
{
    [TestClass]
    public class ElementDefUtilsTests
    {
        [ClassInitialize]
        public static void SetupSource(TestContext t)
        {
            _source = ZipSource.CreateValidationSource();

            var obs = _source.FindStructureDefinitionForCoreType(FHIRDefinedType.Observation);
            _defs = new ElementDefinitionNavigator(obs);
        }

        private static IResourceResolver _source = null;
        private static ElementDefinitionNavigator _defs = null;
        private static ElementDefinitionNavigator getObsDef() => _defs.ShallowCopy();

        [TestMethod]
        public void TestIsRootPath()
        {
            Assert.IsFalse(IsRootPath(""));
            Assert.IsTrue(IsRootPath("Patient"));
            Assert.IsTrue(IsRootPath("integer"));
            Assert.IsFalse(IsRootPath("integer.value"));
            Assert.IsFalse(IsRootPath("Patient.extension.url"));
        }

        [TestMethod]
        public void TestIsRepeating()
        {
            var def = new ElementDefinition().Unbounded();
            Assert.IsTrue(def.IsRepeating());

            def = new ElementDefinition().Prohibited();
            Assert.IsFalse(def.IsRepeating());

            def = new ElementDefinition().Required(1, max: "1");
            Assert.IsFalse(def.IsRepeating());

            def = new ElementDefinition().Required(2, max: "10");
            Assert.IsTrue(def.IsRepeating());
        }

        [TestMethod]
        public void TestIsBackbone()
        {
            var defs = getObsDef();
            Assert.IsTrue(defs.JumpToFirst("Observation.related"));
            Assert.IsTrue(defs.Current.IsBackboneElement());

            Assert.IsTrue(defs.JumpToFirst("Observation.identifier"));
            Assert.IsFalse(defs.Current.IsBackboneElement());
        }

        [TestMethod]
        public void TestDistinctTypeCodes()
        {
            var defs = getObsDef();
            Assert.IsTrue(defs.JumpToFirst("Observation.identifier"));
            CollectionAssert.AreEqual(new[] { FHIRDefinedType.Identifier }, defs.Current.DistinctTypeCodes());

            Assert.IsTrue(defs.JumpToFirst("Observation.effective[x]"));
            CollectionAssert.AreEqual(new[] { FHIRDefinedType.DateTime, FHIRDefinedType.Period }, defs.Current.DistinctTypeCodes());

            var def = new ElementDefinition().Type(FHIRDefinedType.HumanName, "http://fhir/profile1")
                    .Type(FHIRDefinedType.HumanName, "http://fhir/profile2")
                    .Type(FHIRDefinedType.Identifier);

            CollectionAssert.AreEqual(new[] { FHIRDefinedType.HumanName, FHIRDefinedType.Identifier }, def.DistinctTypeCodes());
        }

        [TestMethod]
        public void TestIsReference()
        {
            var refr = new ElementDefinition().Reference("http://fhir/something");
            Assert.IsTrue(refr.IsReference());

            refr = new ElementDefinition().Type(FHIRDefinedType.Identifier);
            Assert.IsFalse(refr.IsReference());
        }

        [TestMethod]
        public void TestHasChoiceSuffix()
        {
            var defs = getObsDef();
            Assert.IsTrue(defs.JumpToFirst("Observation.value[x]"));
            Assert.IsTrue(defs.Current.HasChoiceSuffix());

            Assert.IsTrue(defs.JumpToFirst("Observation.identifier"));
            Assert.IsFalse(defs.Current.HasChoiceSuffix());
        }

        [TestMethod]
        public void TestIsChoiceElement()
        {
            var defs = getObsDef();
            Assert.IsTrue(defs.JumpToFirst("Observation.value[x]"));
            Assert.IsTrue(defs.Current.IsChoiceElement());

            // now, simulate constraint
            var ed = defs.Current.DeepCopy() as ElementDefinition;
            ed.Base = new ElementDefinition.BaseComponent() { Path = ed.Path };
            ed.Path = "Observation.valueQuantity";
            Assert.IsTrue(ed.IsChoiceElement());

            Assert.IsTrue(defs.JumpToFirst("Observation.identifier"));
            Assert.IsFalse(defs.Current.IsChoiceElement());
        }

        [TestMethod]
        public void TestMatchesName()
        {
            var defs = getObsDef();

            Assert.IsTrue(defs.JumpToFirst("Observation.identifier"));
            Assert.IsTrue(defs.Current.MatchesName("identifier"));
            Assert.IsFalse(defs.Current.MatchesName("identifie"));

            Assert.IsTrue(defs.JumpToFirst("Observation.value[x]"));
            Assert.IsTrue(defs.Current.MatchesName("value"));
            Assert.IsFalse(defs.Current.MatchesName("valueQuantity"));
            Assert.IsFalse(defs.Current.MatchesName("val"));

            // now, simulate constraint
            var ed = defs.Current.DeepCopy() as ElementDefinition;
            ed.Base = new ElementDefinition.BaseComponent() { Path = ed.Path };
            ed.Path = "Observation.valueQuantity";
            Assert.IsTrue(ed.MatchesName("value"));
            Assert.IsFalse(ed.MatchesName("valu"));
        }

        [TestMethod]
        public void TestIsPrimitiveValue()
        {
            var defs = getObsDef();
            Assert.IsTrue(defs.JumpToFirst("Observation.identifier"));
            Assert.IsFalse(defs.Current.IsPrimitiveValueConstraint());

            var strDef = _source.FindStructureDefinitionForCoreType(FHIRDefinedType.String);
            defs = new ElementDefinitionNavigator(strDef);
            Assert.IsTrue(defs.JumpToFirst("string.value"));
            Assert.IsTrue(defs.Current.IsPrimitiveValueConstraint());

            var hnDef = _source.FindStructureDefinitionForCoreType(FHIRDefinedType.HumanName);
            defs = new ElementDefinitionNavigator(hnDef);
            Assert.IsTrue(defs.JumpToFirst("HumanName.suffix"));
            Assert.IsFalse(defs.Current.IsPrimitiveValueConstraint());
        }

        [TestMethod]
        public void TestGetNameFromPath()
        {
            Assert.AreEqual("Patient", ElementDefinitionUtilities.GetNameFromPath("Patient"));
            Assert.AreEqual("name", ElementDefinitionUtilities.GetNameFromPath("Patient.name"));
            Assert.AreEqual("value[x]", ElementDefinitionUtilities.GetNameFromPath("Patient.name.value[x]"));
        }
    }
}