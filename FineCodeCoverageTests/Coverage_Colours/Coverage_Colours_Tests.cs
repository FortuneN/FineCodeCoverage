using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Linq.Expressions;

namespace FineCodeCoverageTests
{
    internal abstract class CoverageTypeFilter_Tests_Base<TCoverageTypeFilter> where TCoverageTypeFilter:ICoverageTypeFilter, new()
    {
        public static Action<IAppOptions,bool> GetSetter(Expression<Func<IAppOptions, bool>> propertyGetExpression)
        {
            var entityParameterExpression =
            (ParameterExpression)(((MemberExpression)(propertyGetExpression.Body)).Expression);
            var valueParameterExpression = Expression.Parameter(typeof(bool));

            return Expression.Lambda<Action<IAppOptions, bool>>(
                Expression.Assign(propertyGetExpression.Body, valueParameterExpression),
                entityParameterExpression,
                valueParameterExpression).Compile();
        }
        #region expressions / actions
        protected abstract Expression<Func<IAppOptions,bool>> ShowCoverageExpression { get;}
        protected abstract Expression<Func<IAppOptions, bool>> ShowCoveredExpression { get; }
        protected abstract Expression<Func<IAppOptions, bool>> ShowUncoveredExpression { get; }
        protected abstract Expression<Func<IAppOptions, bool>> ShowPartiallyCoveredExpression { get; }

        private Action<IAppOptions, bool> showCoverage;
        private Action<IAppOptions, bool> ShowCoverage
        {
            get
            {
                if(showCoverage == null)
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
            var appOptions = new Mock<IAppOptions>().SetupAllProperties().Object;
            ShowCoverage(appOptions,true);
            appOptions.ShowEditorCoverage = false;

            coverageTypeFilter.Initialize(appOptions);
            
            Assert.True(coverageTypeFilter.Disabled);
        }

        [Test]
        public void Should_Be_Disabled_When_Show_Coverage_False()
        {
            var coverageTypeFilter = new TCoverageTypeFilter();
            var appOptions = new Mock<IAppOptions>().SetupAllProperties().Object;
            ShowCoverage(appOptions,false);
            appOptions.ShowEditorCoverage = true;

            coverageTypeFilter.Initialize(appOptions);
            Assert.True(coverageTypeFilter.Disabled);
        }

        [TestCase(true, true, true)]
        [TestCase(false, false, false)]
        public void Should_Show_Using_Classification_AppOptions(bool showCovered, bool showUncovered, bool showPartiallyCovered)
        {
            var coverageTypeFilter = new TCoverageTypeFilter();
            var appOptions = new Mock<IAppOptions>().SetupAllProperties().Object;
            ShowCoverage(appOptions,true);
            appOptions.ShowEditorCoverage = true;
            ShowCovered(appOptions,showCovered);
            ShowUncovered(appOptions,showUncovered);
            ShowPartiallyCovered(appOptions,showPartiallyCovered);

            coverageTypeFilter.Initialize(appOptions);

            Assert.That(coverageTypeFilter.Show(CoverageType.Covered), Is.EqualTo(showCovered));
            Assert.That(coverageTypeFilter.Show(CoverageType.NotCovered), Is.EqualTo(showUncovered));
            Assert.That(coverageTypeFilter.Show(CoverageType.Partial), Is.EqualTo(showPartiallyCovered));
        }
    }

    internal class CoverageClassificationFilterX_Tests
    {
        [TestCaseSource(nameof(ChangedTestSource))]
        public void Should_Be_Changed_When_AppOptions_Changed(ChangedTestArguments changedTestArguments)
        {
            var coverageClassificationFilter = new CoverageClassificationFilter();
            coverageClassificationFilter.Initialize(changedTestArguments.InitialAppOptions);
            var newCoverageClassificationFilter = new CoverageClassificationFilter();
            newCoverageClassificationFilter.Initialize(changedTestArguments.ChangedAppOptions);
            
            Assert.That(newCoverageClassificationFilter.Changed(coverageClassificationFilter), Is.EqualTo(changedTestArguments.ExpectedChanged));
        }

        internal class ChangedTestArguments
        {
            public ChangedTestArguments(IAppOptions initialAppOptions, IAppOptions changedAppOptions, bool expectedChanged)
            {
                InitialAppOptions = initialAppOptions;
                ChangedAppOptions = changedAppOptions;
                ExpectedChanged = expectedChanged;
            }
            public IAppOptions InitialAppOptions { get; }
            public IAppOptions ChangedAppOptions { get; }
            public bool ExpectedChanged { get; }

            public static IAppOptions Create(
                bool showLineCoveredHighlighting,
                bool showLineUncoveredHighlighting,
                bool showLinePartiallyCoveredHighlighting,
                bool showEditorCoverage = true,
                bool showLineCoverageHighlighting = true
                )
            {
                var appOptions = new Mock<IAppOptions>().SetupAllProperties().Object;

                appOptions.ShowEditorCoverage = showEditorCoverage;
                appOptions.ShowLineCoverageHighlighting = showLineCoverageHighlighting;

                appOptions.ShowLineCoveredHighlighting = showLineCoveredHighlighting;
                appOptions.ShowLineUncoveredHighlighting = showLineUncoveredHighlighting;
                appOptions.ShowLinePartiallyCoveredHighlighting = showLinePartiallyCoveredHighlighting;

                return appOptions;
            }

        }

        public static ChangedTestArguments[] ChangedTestSource =
        
            new ChangedTestArguments[]{
                new ChangedTestArguments(
                    ChangedTestArguments.Create(true,true,true),
                    ChangedTestArguments.Create(false,true,true),
                    true
                ),
                new ChangedTestArguments(
                    ChangedTestArguments.Create(true,true,true),
                    ChangedTestArguments.Create(true,false,true),
                    true
                ),
                new ChangedTestArguments(
                    ChangedTestArguments.Create(true,true,true),
                    ChangedTestArguments.Create(true,true,false),
                    true
                ),
                new ChangedTestArguments(
                    ChangedTestArguments.Create(true,true,true,true,true),
                    ChangedTestArguments.Create(true,true,true,false,true),
                    true
                ),
                new ChangedTestArguments(
                    ChangedTestArguments.Create(true,true,true,true,true),
                    ChangedTestArguments.Create(true,true,true,true,false),
                    true
                ),
                new ChangedTestArguments(
                    ChangedTestArguments.Create(true,true,true),
                    ChangedTestArguments.Create(true,true,true),
                    false
                )
            
        };
    }    
}