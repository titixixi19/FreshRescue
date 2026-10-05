namespace FreshRescue.Entities.Enums;

/// <summary>Quyết định biên độ giảm giá tầng sàn (BR-07).</summary>
public enum OfferType : byte
{
    /// <summary>Túi mù: giảm 65–75%.</summary>
    SurpriseBag = 1,

    /// <summary>Món lẻ xả kho: giảm 40–50%.</summary>
    ClearanceItem = 2,
}
