namespace CafeManager.Tests.G03;
public class RunnerControlledFailure
{
    [Xunit.Fact]
    public void ControlledFailure() => Xunit.Assert.Fail("G03-B01 controlled unit failure");
}
