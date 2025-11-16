using FluentAssertions;
using Shared.Validation;

namespace EventService.Tests;

public class EventSchemaValidatorTests
{
    private readonly EventSchemaValidator _validator;

    public EventSchemaValidatorTests()
    {
        var schema = EventSchemaLoader.LoadFromEmbeddedResource();
        _validator = new EventSchemaValidator(schema);
    }

    [Fact]
    public void ValidEvent_ShouldPass()
    {
        var json = """
        {
          "event_id": "3e0ab897-2667-42c8-88e6-b3eb5fe2715f",
          "source": "email",
          "event_type": "email_received",
          "timestamp": "2025-11-16T17:30:00Z",
          "schema_version": "1",
          "payload": {
              "from": "billing@utility.com",
              "subject": "Your monthly bill"
          }
        }
        """;

        var result = _validator.Validate(json);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MissingRequiredField_ShouldFail()
    {
        var json = """
        {
          "source": "email",
          "event_type": "email_received",
          "timestamp": "2025-11-16T17:30:00Z",
          "schema_version": "1",
          "payload": {}
        }
        """;

        var result = _validator.Validate(json);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void InvalidEnum_ShouldFail()
    {
        var json = """
        {
          "event_id": "3e0ab897-2667-42c8-88e6-b3eb5fe2715f",
          "source": "gmail",
          "event_type": "email_received",
          "timestamp": "2025-11-16T17:30:00Z",
          "schema_version": "1",
          "payload": {}
        }
        """;

        var result = _validator.Validate(json);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AdditionalProperty_ShouldFail()
    {
        var json = """
        {
          "event_id": "3e0ab897-2667-42c8-88e6-b3eb5fe2715f",
          "source": "email",
          "event_type": "email_received",
          "timestamp": "2025-11-16T17:30:00Z",
          "schema_version": "1",
          "payload": {},
          "unexpected": "extra"
        }
        """;

        var result = _validator.Validate(json);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
