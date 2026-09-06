namespace RxMvvmLight.Validation;

public enum ValidatorState
{
    /// <summary>
    /// 初始值，尚未验证
    /// </summary>
    NotValidated,
    /// <summary>
    /// 正在评估
    /// </summary>
    Validating,
    Valid,
    Invalid
}
