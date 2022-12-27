using System.Linq;
using UnityEngine;
using UniVRM10;

namespace uLipSync
{

[ExecuteAlways]
[RequireComponent(typeof(Vrm10Instance))]
public class uLipSyncExpressionVRM : uLipSyncBlendShape
{
    protected override void OnApplyBlendShapes()
    {
        var vrm10Instance = GetComponent<Vrm10Instance>();
        if (!vrm10Instance || !vrm10Instance.Vrm) return;

        var clips = vrm10Instance.Vrm.Expression.Clips.ToArray();
        foreach (var bs in blendShapes)
        {
            var index = bs.index + 1;
            if (index < 0 || index >= clips.Length) continue;
            var clip = clips[index];
            var weight = bs.weight * bs.maxWeight * volume;
            vrm10Instance.Runtime.Expression.SetWeight(new ExpressionKey(clip.Preset, clip.Clip.name), weight);
        }
    }
}

}