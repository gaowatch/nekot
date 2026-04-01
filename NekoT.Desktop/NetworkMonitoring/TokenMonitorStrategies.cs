using System;

namespace NekoT.Desktop.NetworkMonitoring;

public interface ITokenMonitorStrategy
{
    string ProviderName { get; }
    AccuracyLevel Accuracy { get; }
    
    void OnTokenDetected(TokenExtractedEventArgs e);
    void OnStreamingDataReceived(string data, string requestId);
    int GetCurrentTokens();
    int GetPredictedTokens();
    void Reset();
}

public abstract class TokenMonitorStrategyBase : ITokenMonitorStrategy
{
    protected int _currentTokens = 0;
    protected string _currentRequestId = string.Empty;
    
    public abstract string ProviderName { get; }
    public abstract AccuracyLevel Accuracy { get; }
    
    public virtual void OnTokenDetected(TokenExtractedEventArgs e)
    {
        _currentTokens = e.Tokens;
        _currentRequestId = e.RequestUrl;
    }
    
    public virtual void OnStreamingDataReceived(string data, string requestId)
    {
    }
    
    public virtual int GetCurrentTokens()
    {
        return _currentTokens;
    }
    
    public virtual int GetPredictedTokens()
    {
        return _currentTokens;
    }
    
    public virtual void Reset()
    {
        _currentTokens = 0;
        _currentRequestId = string.Empty;
    }
}

public class PreciseTokenMonitorStrategy : TokenMonitorStrategyBase
{
    private readonly string _providerName;
    
    public PreciseTokenMonitorStrategy(string providerName)
    {
        _providerName = providerName;
    }
    
    public override string ProviderName => _providerName;
    public override AccuracyLevel Accuracy => AccuracyLevel.Precise;
    
    public override void OnTokenDetected(TokenExtractedEventArgs e)
    {
        base.OnTokenDetected(e);
        System.Diagnostics.Debug.WriteLine($"[PreciseStrategy] {_providerName}: {e.Tokens} tokens (精确)");
    }
}

public class EstimatedTokenMonitorStrategy : TokenMonitorStrategyBase
{
    private readonly string _providerName;
    private readonly double _accuracyFactor;
    
    public EstimatedTokenMonitorStrategy(string providerName, double accuracyFactor = 0.85)
    {
        _providerName = providerName;
        _accuracyFactor = accuracyFactor;
    }
    
    public override string ProviderName => _providerName;
    public override AccuracyLevel Accuracy => AccuracyLevel.Estimated;
    
    public override void OnTokenDetected(TokenExtractedEventArgs e)
    {
        base.OnTokenDetected(e);
        System.Diagnostics.Debug.WriteLine($"[EstimatedStrategy] {_providerName}: {e.Tokens} tokens (估算，准确率 ~{_accuracyFactor:P0})");
    }
    
    public override int GetPredictedTokens()
    {
        return (int)(_currentTokens / _accuracyFactor);
    }
}

public class NotSupportedTokenMonitorStrategy : TokenMonitorStrategyBase
{
    private readonly string _providerName;
    private readonly string _reason;
    
    public NotSupportedTokenMonitorStrategy(string providerName, string reason)
    {
        _providerName = providerName;
        _reason = reason;
    }
    
    public override string ProviderName => _providerName;
    public override AccuracyLevel Accuracy => AccuracyLevel.NotSupported;
    
    public override void OnTokenDetected(TokenExtractedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[NotSupportedStrategy] {_providerName}: 不支持监控 - {_reason}");
    }
}

public class DefaultTokenMonitorStrategy : TokenMonitorStrategyBase
{
    public override string ProviderName => "Unknown";
    public override AccuracyLevel Accuracy => AccuracyLevel.Unknown;
}