using Atendefy.API.SharedKernel;
using FluentAssertions;

namespace Atendefy.Tests.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Ok_ShouldReturnSuccessResult()
    {
        var result = Result<string>.Ok("hello");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Fail_ShouldReturnFailureResult()
    {
        var result = Result<string>.Fail("something went wrong");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("something went wrong");
    }

    [Fact]
    public void Ok_WithNoValue_ShouldReturnSuccess()
    {
        var result = Result.Ok();
        result.IsSuccess.Should().BeTrue();
    }
}
