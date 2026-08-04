using FluentAssertions;
using Shared.Domain.Primitives;
using Shared.Domain.Security;

namespace Shared.Domain.UnitTests;

public sealed class PrimitivesEqualityTests
{
    private sealed class TestEntity(Guid id) : Entity<Guid>(id);

    private sealed class TestEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Raise(IDomainEvent e) => RaiseDomainEvent(e);
    }

    [Fact]
    public void Entities_with_same_id_are_equal()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());
        TestEntity? nothing = null;

        a.Equals(b).Should().BeFalse();
        (a == b).Should().BeFalse();
        (a == nothing).Should().BeFalse();
        a.Equals(nothing).Should().BeFalse();
    }

    [Fact]
    public void Aggregate_collects_and_clears_domain_events()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var e = new TestEvent();

        aggregate.DomainEvents.Should().BeEmpty();
        aggregate.Raise(e);
        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(e);
        aggregate.ClearDomainEvents();
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Actors_equality_by_components()
    {
        var id = Guid.NewGuid();
        Actor? nothing = null;

        Actor.Staff(id).Should().Be(Actor.Staff(id));
        (Actor.Staff(id) == Actor.From(ActorType.Staff, id)).Should().BeTrue();
        (Actor.Staff(id) != Actor.System).Should().BeTrue();
        Actor.Staff(id).GetHashCode().Should().Be(Actor.Staff(id).GetHashCode());
        Actor.System.Equals(nothing).Should().BeFalse();
    }

    [Fact]
    public void Actor_factories_and_tostring()
    {
        var id = Guid.NewGuid();

        Actor.System.Type.Should().Be(ActorType.System);
        Actor.System.Id.Should().BeNull();
        Actor.System.ToString().Should().Be("System");
        Actor.Customer(id).Type.Should().Be(ActorType.Customer);
        Actor.Staff(id).ToString().Should().Be($"Staff:{id}");
    }
}
