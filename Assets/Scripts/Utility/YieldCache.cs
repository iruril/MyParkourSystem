using System.Collections.Generic;
using UnityEngine;

public static class YieldCache
{
    /// <summary>
    /// float 비교연산 간에 혹시모를 GC호출을 방, 그리고 더 빠른 비교를 가능하게 하는 인터페이스 클래스
    /// </summary>
    private class FloatComparer : IEqualityComparer<float>
    {
        bool IEqualityComparer<float>.Equals(float x, float y)
        {
            return x == y;
        }
        int IEqualityComparer<float>.GetHashCode(float obj)
        {
            return obj.GetHashCode();
        }
    }

    public static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
    public static readonly WaitForFixedUpdate WaitForFixedUpdate = new WaitForFixedUpdate();

    private static readonly Dictionary<float, WaitForSeconds> _wfsPool =
        new Dictionary<float, WaitForSeconds>(new FloatComparer());
    private static readonly Dictionary<float, WaitForSecondsRealtime> _wfsRealTimePool =
        new Dictionary<float, WaitForSecondsRealtime>(new FloatComparer());

    /// <summary>
    /// WFS를 반환해주는 함수. 일단 풀에 먼저 접근해서 존재하면 그것을 반환하고, 아니면 새로 생성해서 등록한다
    /// </summary>
    /// <param name="seconds">WFS 딜레이 시간</param>
    /// <returns>딜레이를 줄 WaitForSeconds</returns>
    public static WaitForSeconds WaitForSeconds(float seconds)
    {
        WaitForSeconds wfs;
        if (!_wfsPool.TryGetValue(seconds, out wfs))
            _wfsPool.Add(seconds, wfs = new WaitForSeconds(seconds));
        return wfs;
    }

    /// <summary>
    /// WFS_RealTime을 반환해주는 함수. 일단 풀에 먼저 접근해서 존재하면 그것을 반환하고, 아니면 새로 생성해서 등록한다
    /// </summary>
    /// <param name="seconds">WFS_RealTime 딜레이 시간</param>
    /// <returns>딜레이를 줄 WaitForSecondsRealTime</returns>
    public static WaitForSecondsRealtime WaitForSecondsRealTime(float seconds)
    {
        WaitForSecondsRealtime wfsReal;
        if (!_wfsRealTimePool.TryGetValue(seconds, out wfsReal))
            _wfsRealTimePool.Add(seconds, wfsReal = new WaitForSecondsRealtime(seconds));
        return wfsReal;
    }
}
