using RecipeManager.Domain.Shared;
using Xunit;

namespace RecipeManager.Domain.Tests.Shared
{
    public class EntityTests
    {
        private class TestEntity : Entity
        {
            public TestEntity(Guid id)
            {
                Id = id;
            }
        }

        private class AnotherEntity : Entity
        {
            public AnotherEntity(Guid id)
            {
                Id = id;
            }
        }

        [Fact]
        public void Equals_SameReference_ReturnsTrue()
        {
            var id = Guid.NewGuid();
            var e = new TestEntity(id);
            Assert.True(e.Equals(e));
        }

        [Fact]
        public void Equals_SameIdAndType_ReturnsTrue()
        {
            var id = Guid.NewGuid();
            var a = new TestEntity(id);
            var b = new TestEntity(id);
            Assert.True(a.Equals(b));
            Assert.True(b.Equals(a));
        }

        [Fact]
        public void Equals_DifferentIdSameType_ReturnsFalse()
        {
            var a = new TestEntity(Guid.NewGuid());
            var b = new TestEntity(Guid.NewGuid());
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_SameIdDifferentType_ReturnsFalse()
        {
            var id = Guid.NewGuid();
            var a = new TestEntity(id);
            var b = new AnotherEntity(id);
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_NullOrOtherType_ReturnsFalse()
        {
            var e = new TestEntity(Guid.NewGuid());
            Assert.False(e.Equals(null));
            Assert.False(e.Equals("Not an entity"));
        }

        [Fact]
        public void GetHashCode_IsConsistentWithIdHashCode()
        {
            var id = Guid.NewGuid();
            var e = new TestEntity(id);
            Assert.Equal(id.GetHashCode(), e.GetHashCode());
        }
    }
}
