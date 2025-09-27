namespace CaliphAuctionBackend.Utils;

public static class MathUtils {
	/// <summary>
	///     指定した確率で true を返します（確率は 0.0〜1.0 の範囲）。
	/// </summary>
	/// <param name="probability">0.0（0%）から1.0（100%）の確率</param>
	/// <returns>指定確率で true、そうでなければ false</returns>
	public static bool Chance(double probability) {
		if (double.IsNaN(probability)) {
			throw new ArgumentException("probability must be a number", nameof(probability));
		}

		return Random.Shared.NextDouble() < probability;
	}
}