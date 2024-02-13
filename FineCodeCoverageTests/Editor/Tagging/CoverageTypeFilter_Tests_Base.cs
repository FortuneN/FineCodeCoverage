using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Linq.Expressions;

namespace FineCodeCoverageTests.Editor.Tagging.CoverageTypeFilter
{
    internal abstract class CoverageTypeFilter_Tests_Base<TCoverageTypeFilter> where TCoverageTypeFilter : ICoverageTypeFilter, new()
    {
        public static Action<IAppOptions, bool> GetSetter(Expression<Func<IAppOptions, bool>> propertyGetExpression)
        {
            var entityParameterExpression =
            (ParameterExpression)((MemberExpression)propertyGetExpression.Body).Expression;
            var valueParameterExpression = Expression.Parameter(typeof(bool));

            return Expression.Lambda<Action<IAppOptions, bool>>(
                Expression.Assign(propertyGetExpression.Body, valueParameterExpression),
                entityParameterExpression,
                valueParameterExpression).Compile();
        }
        #region expressions / actions
        protected abstract Expression<Func<IAppOptions, bool>> ShowCoverageExpression { get; }
        protected abstract Expression<Func<IAppOptions, bool>> ShowCoveredExpression { get; }
        protected abstract Expression<Func<IAppOptions, bool>> ShowUncoveredExpression { get; }
        protected abstract Expression<Func<IAppOptions, bool>> ShowPartiallyCoveredExpression { get; }

        private Action<IAppOptions, bool> showCoverage;
        private Action<IAppOptions, bool> ShowCoverage
        {
            get
            {
                if (showCoverage == null)
                {
                    showCoverage = GetSetter(ShowCoverageExpression);
                }
                return showCoverage;
            }
        }
        private Action<IAppOptions, bool> showCovered;
        private Action<IAppOptions, bool> ShowCovered
        {
            get
            {
                if (showCovered == null)
                {
                    showCovered = GetSetter(ShowCoveredExpression);
                }
                return showCovered;
            }
        }
        private Action<IAppOptions, bool> showUncovered;
        private Action<IAppOptions, bool> ShowUncovered
        {
            get
            {
                if (showUncovered == null)
                {
                    showUncovered = GetSetter(ShowUncoveredExpression);
                }
                return showUncovered;
            }
        }
        private Action<IAppOptions, bool> showPartiallyCovered;
        private Action<IAppOptions, bool> ShowPartiallyCovered
        {
            get
            {
                if (showPartiallyCovered == null)
                {
                    showPartiallyCovered = GetSetter(ShowPartiallyCoveredExpression);
                }
                return showPartiallyCovered;
            }
        }
        #endregion

        [Test]
        public void Should_Be_Disabled_When_ShowEditorCoverage_False()
        {
            var coverageTypeFilter = new TCoverageTypeFilter();
            var appOptions = GetAppOptions();
            ShowCoverage(appOptions, true);
            appOptions.ShowEditorCoverage = false;

            coverageTypeFilter.Initialize(appOptions);

            Assert.True(coverageTypeFilter.Disabled);
        }

        [Test]
        public void Should_Be_Disabled_When_Show_Coverage_False()
        {
            var coverageTypeFilter = new TCoverageTypeFilter();
            var appOptions = GetAppOptions();
            ShowCoverage(appOptions, false);
            appOptions.ShowEditorCoverage = true;

            coverageTypeFilter.Initialize(appOptions);
            Assert.True(coverageTypeFilter.Disabled);
        }

        private IAppOptions GetAppOptions()
        {
            return new Mock<IAppOptions>().SetupAllProperties().Object;
        }

        [TestCase(true, true, true)]
        [TestCase(false, false, false)]
        public void Should_Show_Using_Classification_AppOptions(bool showCovered, bool showUncovered, bool showPartiallyCovered)
        {
            //var coverageTypeFilter = new TCoverageTypeFilter();
            //var appOptions = new Mock<IAppOptions>().SetupAllProperties().Object;
            //ShowCoverage(appOptions, true);
            //appOptions.ShowEditorCoverage = true;
            //ShowCovered(appOptions, showCovered);
            //ShowUncovered(appOptions, showUncovered);
            //ShowPartiallyCovered(appOptions, showPartiallyCovered);

            //coverageTypeFilter.Initialize(appOptions);

            //Assert.That(coverageTypeFilter.Show(CoverageType.Covered), Is.EqualTo(showCovered));
            //Assert.That(coverageTypeFilter.Show(CoverageType.NotCovered), Is.EqualTo(showUncovered));
            //Assert.That(coverageTypeFilter.Show(CoverageType.Partial), Is.EqualTo(showPartiallyCovered));
            throw new System.NotImplementedException();
        }

        [TestCaseSource(nameof(ChangedTestSource))]
        public void Should_Be_Changed_When_AppOptions_Changed(ChangedTestArguments changedTestArguments)
        {
            var coverageTypeFilter = new TCoverageTypeFilter();
            coverageTypeFilter.Initialize(SetAppOptions(changedTestArguments.InitialAppOptions));
            var newCoverageTypeFilter = new TCoverageTypeFilter();
            newCoverageTypeFilter.Initialize(SetAppOptions(changedTestArguments.ChangedAppOptions));

            Assert.That(newCoverageTypeFilter.Changed(coverageTypeFilter), Is.EqualTo(changedTestArguments.ExpectedChanged));
        }

        private IAppOptions SetAppOptions(CoverageAppOptions coverageAppOptions)
        {
            var appOptions = GetAppOptions();
            appOptions.ShowEditorCoverage = coverageAppOptions.ShowEditorCoverage;
            ShowCoverage(appOptions, coverageAppOptions.ShowCoverage);
            ShowCovered(appOptions, coverageAppOptions.ShowCovered);
            ShowUncovered(appOptions, coverageAppOptions.ShowUncovered);
            ShowPartiallyCovered(appOptions, coverageAppOptions.ShowPartiallyCovered);
            return appOptions;
        }

        public class CoverageAppOptions
        {
            public bool ShowCoverage { get; set; }
            public bool ShowCovered { get; set; }
            public bool ShowUncovered { get; set; }
            public bool ShowPartiallyCovered { get; set; }
            public bool ShowEditorCoverage { get; set; }
            public CoverageAppOptions(
                bool showCovered,
                bool showUncovered,
                bool showPartiallyCovered,
                bool showEditorCoverage = true,
                bool showCoverage = true
            )
            {
                ShowCoverage = showCoverage;
                ShowCovered = showCovered;
                ShowUncovered = showUncovered;
                ShowPartiallyCovered = showPartiallyCovered;
                ShowEditorCoverage = showEditorCoverage;
            }
        }

        internal class ChangedTestArguments
        {

            public ChangedTestArguments(CoverageAppOptions initialAppOptions, CoverageAppOptions changedAppOptions, bool expectedChanged)
            {
                InitialAppOptions = initialAppOptions;
                ChangedAppOptions = changedAppOptions;
                ExpectedChanged = expectedChanged;
            }
            public CoverageAppOptions InitialAppOptions { get; }
            public CoverageAppOptions ChangedAppOptions { get; }
            public bool ExpectedChanged { get; }
        }

        public static ChangedTestArguments[] ChangedTestSource = new ChangedTestArguments[]{
                new ChangedTestArguments(
                    new CoverageAppOptions(true,true,true),
                    new CoverageAppOptions(false,true,true),
                    true
                ),
                new ChangedTestArguments(
                    new CoverageAppOptions(true,true,true),
                    new CoverageAppOptions(true,false,true),
                    true
                ),
                new ChangedTestArguments(
                    new CoverageAppOptions(true,true,true),
                    new CoverageAppOptions(true,true,false),
                    true
                ),
                new ChangedTestArguments(
                    new CoverageAppOptions(true,true,true,true,true),
                    new CoverageAppOptions(true,true,true,false,true),
                    true
                ),
                new ChangedTestArguments(
                    new CoverageAppOptions(true,true,true,true,true),
                    new CoverageAppOptions(true,true,true,true,false),
                    true
                ),
                new ChangedTestArguments(
                    new CoverageAppOptions(true,true,true),
                    new CoverageAppOptions(true,true,true),
                    false
                )
        };
    }

}