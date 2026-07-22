using Amazon.Lambda.Core;

namespace Oficina.Auth.Authorizer.Tests;

internal sealed class TestLambdaContext : ILambdaContext
{
    public string AwsRequestId { get; set; } = "test-request";
    public IClientContext ClientContext { get; set; } = null!;
    public string FunctionName { get; set; } = "test-function";
    public string FunctionVersion { get; set; } = "$LATEST";
    public ICognitoIdentity Identity { get; set; } = null!;
    public string InvokedFunctionArn { get; set; } = "arn:aws:lambda:us-east-1:123456789012:function:test";
    public ILambdaLogger Logger { get; set; } = new TestLambdaLogger();
    public string LogGroupName { get; set; } = "/aws/lambda/test";
    public string LogStreamName { get; set; } = "test";
    public int MemoryLimitInMB { get; set; } = 256;
    public TimeSpan RemainingTime { get; set; } = TimeSpan.FromSeconds(30);

    private sealed class TestLambdaLogger : ILambdaLogger
    {
        public void Log(string message)
        {
        }

        public void LogLine(string message)
        {
        }
    }
}
