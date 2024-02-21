﻿using KoeBook.Core.Models;

namespace KoeBook.Core.Contracts.Services;

public interface IDisplayStateChangeService
{
    /// <summary>
    /// 状態を更新します
    /// </summary>
    void UpdateState(BookProperties bookProperties, GenerationState state);

    /// <summary>
    /// プログレスバーを更新します
    /// </summary>
    void UpdateProgress(BookProperties bookProperties, int progress, int maximum);
}
