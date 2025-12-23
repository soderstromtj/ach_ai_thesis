using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Persistence.Entities;
using NIU.ACH_AI.Infrastructure.Persistence.Mappers;

namespace NIU.ACH_AI.Infrastructure.Tests.Persistence.Mappers
{
    public class HypothesisMapperTests
    {
        #region ToDatabase Tests

        [Fact]
        public void ToDatabase_WithValidInputAndIsRefinedTrue_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var hypothesis = new Hypothesis
            {
                ShortTitle = "Test Hypothesis",
                HypothesisText = "This is a test hypothesis for validation"
            };
            var stepExecutionId = Guid.NewGuid();
            var isRefined = true;
            var beforeMapping = DateTime.UtcNow;

            // Act
            var result = HypothesisMapper.ToDatabase(hypothesis, stepExecutionId, isRefined);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.HypothesisId);
            Assert.Equal(stepExecutionId, result.StepExecutionId);
            Assert.Equal("Test Hypothesis", result.ShortTitle);
            Assert.Equal("This is a test hypothesis for validation", result.HypothesisText);
            Assert.True(result.IsRefined);
            Assert.True(result.CreatedAt >= beforeMapping);
            Assert.True(result.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void ToDatabase_WithValidInputAndIsRefinedFalse_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var hypothesis = new Hypothesis
            {
                ShortTitle = "Unrefined Hypothesis",
                HypothesisText = "This hypothesis has not been refined"
            };
            var stepExecutionId = Guid.NewGuid();
            var isRefined = false;

            // Act
            var result = HypothesisMapper.ToDatabase(hypothesis, stepExecutionId, isRefined);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.HypothesisId);
            Assert.Equal(stepExecutionId, result.StepExecutionId);
            Assert.Equal("Unrefined Hypothesis", result.ShortTitle);
            Assert.Equal("This hypothesis has not been refined", result.HypothesisText);
            Assert.False(result.IsRefined);
        }

        [Fact]
        public void ToDatabase_WithNullHypothesis_ThrowsArgumentNullException()
        {
            // Arrange
            Hypothesis? hypothesis = null;
            var stepExecutionId = Guid.NewGuid();
            var isRefined = false;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                HypothesisMapper.ToDatabase(hypothesis!, stepExecutionId, isRefined));
            Assert.Equal("hypothesis", exception.ParamName);
        }

        [Fact]
        public void ToDatabase_WithEmptyStepExecutionId_ThrowsArgumentException()
        {
            // Arrange
            var hypothesis = new Hypothesis
            {
                ShortTitle = "Test",
                HypothesisText = "Test"
            };
            var stepExecutionId = Guid.Empty;
            var isRefined = false;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                HypothesisMapper.ToDatabase(hypothesis, stepExecutionId, isRefined));
            Assert.Equal("stepExecutionId", exception.ParamName);
            Assert.Contains("cannot be empty", exception.Message);
        }

        [Fact]
        public void ToDatabase_WithNullShortTitle_MapsToEmptyString()
        {
            // Arrange
            var hypothesis = new Hypothesis
            {
                ShortTitle = null!,
                HypothesisText = "Test text"
            };
            var stepExecutionId = Guid.NewGuid();
            var isRefined = false;

            // Act
            var result = HypothesisMapper.ToDatabase(hypothesis, stepExecutionId, isRefined);

            // Assert
            Assert.Equal(string.Empty, result.ShortTitle);
        }

        [Fact]
        public void ToDatabase_WithNullHypothesisText_MapsToEmptyString()
        {
            // Arrange
            var hypothesis = new Hypothesis
            {
                ShortTitle = "Test",
                HypothesisText = null!
            };
            var stepExecutionId = Guid.NewGuid();
            var isRefined = false;

            // Act
            var result = HypothesisMapper.ToDatabase(hypothesis, stepExecutionId, isRefined);

            // Assert
            Assert.Equal(string.Empty, result.HypothesisText);
        }

        [Fact]
        public void ToDatabase_GeneratesUniqueIds_ForMultipleCalls()
        {
            // Arrange
            var hypothesis = new Hypothesis
            {
                ShortTitle = "Test",
                HypothesisText = "Test"
            };
            var stepExecutionId = Guid.NewGuid();
            var isRefined = false;

            // Act
            var result1 = HypothesisMapper.ToDatabase(hypothesis, stepExecutionId, isRefined);
            var result2 = HypothesisMapper.ToDatabase(hypothesis, stepExecutionId, isRefined);

            // Assert
            Assert.NotEqual(result1.HypothesisId, result2.HypothesisId);
        }

        [Fact]
        public void ToDatabase_WithLongText_MapsCorrectly()
        {
            // Arrange
            var longText = new string('A', 5000);
            var hypothesis = new Hypothesis
            {
                ShortTitle = "Long hypothesis",
                HypothesisText = longText
            };
            var stepExecutionId = Guid.NewGuid();
            var isRefined = true;

            // Act
            var result = HypothesisMapper.ToDatabase(hypothesis, stepExecutionId, isRefined);

            // Assert
            Assert.Equal(longText, result.HypothesisText);
        }

        [Fact]
        public void ToDatabase_WithEmptyStrings_MapsCorrectly()
        {
            // Arrange
            var hypothesis = new Hypothesis
            {
                ShortTitle = string.Empty,
                HypothesisText = string.Empty
            };
            var stepExecutionId = Guid.NewGuid();
            var isRefined = false;

            // Act
            var result = HypothesisMapper.ToDatabase(hypothesis, stepExecutionId, isRefined);

            // Assert
            Assert.Equal(string.Empty, result.ShortTitle);
            Assert.Equal(string.Empty, result.HypothesisText);
        }

        [Fact]
        public void ToDatabase_WithSpecialCharacters_MapsCorrectly()
        {
            // Arrange
            var hypothesis = new Hypothesis
            {
                ShortTitle = "Test with special chars: <>&\"'",
                HypothesisText = "Text with unicode: 你好 مرحبا שלום"
            };
            var stepExecutionId = Guid.NewGuid();
            var isRefined = true;

            // Act
            var result = HypothesisMapper.ToDatabase(hypothesis, stepExecutionId, isRefined);

            // Assert
            Assert.Equal("Test with special chars: <>&\"'", result.ShortTitle);
            Assert.Equal("Text with unicode: 你好 مرحبا שלום", result.HypothesisText);
        }

        #endregion

        #region ToDomain Tests

        [Fact]
        public void ToDomain_WithValidEntity_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var entity = new HypothesisEntity
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Database Hypothesis",
                HypothesisText = "This is a hypothesis from the database",
                IsRefined = true,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = HypothesisMapper.ToDomain(entity);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Database Hypothesis", result.ShortTitle);
            Assert.Equal("This is a hypothesis from the database", result.HypothesisText);
        }

        [Fact]
        public void ToDomain_WithNullEntity_ThrowsArgumentNullException()
        {
            // Arrange
            HypothesisEntity? entity = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                HypothesisMapper.ToDomain(entity!));
            Assert.Equal("entity", exception.ParamName);
        }

        [Fact]
        public void ToDomain_WithEmptyStrings_MapsCorrectly()
        {
            // Arrange
            var entity = new HypothesisEntity
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = string.Empty,
                HypothesisText = string.Empty,
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = HypothesisMapper.ToDomain(entity);

            // Assert
            Assert.Equal(string.Empty, result.ShortTitle);
            Assert.Equal(string.Empty, result.HypothesisText);
        }

        [Fact]
        public void ToDomain_WithSpecialCharacters_MapsCorrectly()
        {
            // Arrange
            var entity = new HypothesisEntity
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Special chars: <>&\"'",
                HypothesisText = "Unicode: 你好 مرحبا שלום",
                IsRefined = true,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = HypothesisMapper.ToDomain(entity);

            // Assert
            Assert.Equal("Special chars: <>&\"'", result.ShortTitle);
            Assert.Equal("Unicode: 你好 مرحبا שלום", result.HypothesisText);
        }

        #endregion

        #region Round-Trip Tests

        [Fact]
        public void RoundTrip_ToDatabaseAndToDomain_PreservesData()
        {
            // Arrange
            var originalHypothesis = new Hypothesis
            {
                ShortTitle = "Round Trip Test",
                HypothesisText = "Testing round-trip data preservation"
            };
            var stepExecutionId = Guid.NewGuid();
            var isRefined = true;

            // Act
            var entity = HypothesisMapper.ToDatabase(originalHypothesis, stepExecutionId, isRefined);
            var roundTrippedHypothesis = HypothesisMapper.ToDomain(entity);

            // Assert
            Assert.Equal(originalHypothesis.ShortTitle, roundTrippedHypothesis.ShortTitle);
            Assert.Equal(originalHypothesis.HypothesisText, roundTrippedHypothesis.HypothesisText);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ToDatabase_WithDifferentIsRefinedValues_StoresCorrectly(bool isRefined)
        {
            // Arrange
            var hypothesis = new Hypothesis
            {
                ShortTitle = "Test",
                HypothesisText = "Test"
            };
            var stepExecutionId = Guid.NewGuid();

            // Act
            var result = HypothesisMapper.ToDatabase(hypothesis, stepExecutionId, isRefined);

            // Assert
            Assert.Equal(isRefined, result.IsRefined);
        }

        #endregion
    }
}
