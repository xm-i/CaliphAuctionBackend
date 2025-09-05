namespace PennyAuctionBackend.Models;

/// <summary>
/// 種別: ポイントトランザクションの高レベル分類。
/// 購入/消費/返金/失効/調整/ボーナス付与 など。
/// </summary>
public enum PointTransactionType {
	/// <summary>ユーザーがポイントを購入または外部課金完了で加算</summary>
	Purchase = 1,
	/// <summary>ユーザーが入札等でポイントを消費</summary>
	Spend = 2,
	/// <summary>消費の取り消し等でポイントが戻る</summary>
	Refund = 3,
	/// <summary>期限切れにより減算</summary>
	Expire = 4,
	/// <summary>管理者調整(手動加減算)</summary>
	Adjust = 5,
	/// <summary>キャンペーン等のボーナス付与</summary>
	BonusGrant = 6
}