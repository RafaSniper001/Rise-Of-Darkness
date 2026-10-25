using UnityEditor;
using UnityEngine;

// Mede, nos clipes "no lugar" do Mixamo, a velocidade com que o personagem andaria:
// o pé apoiado no chão desliza para trás na mesma velocidade em que o corpo deveria avançar.
public static class GaitMeasure
{
    // velocidade (m/s no mundo, já com a escala do modelo) para os pés não patinarem
    public static float GroundSpeed(GameObject visual, Animator animator, AnimationClip clip)
    {
        var feet = new[] { animator.GetBoneTransform(HumanBodyBones.LeftFoot), animator.GetBoneTransform(HumanBodyBones.RightFoot) };
        const float fps = 60f;
        int frames = Mathf.Max(2, Mathf.RoundToInt(clip.length * fps));
        var height = new float[2, frames + 1];
        var forward = new float[2, frames + 1];

        bool started = AnimationMode.InAnimationMode();
        if (!started) AnimationMode.StartAnimationMode();
        for (int f = 0; f <= frames; f++)
        {
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(visual, clip, clip.length * f / frames);
            AnimationMode.EndSampling();
            for (int s = 0; s < 2; s++)
            {
                Vector3 p = feet[s].position - visual.transform.position;
                height[s, f] = p.y;
                forward[s, f] = Vector3.Dot(p, visual.transform.forward);
            }
        }
        if (!started) AnimationMode.StopAnimationMode();

        // média da velocidade para trás do pé enquanto ele está perto do ponto mais baixo
        float sum = 0f;
        int count = 0;
        for (int s = 0; s < 2; s++)
        {
            float min = float.MaxValue, max = float.MinValue;
            for (int f = 0; f <= frames; f++) { min = Mathf.Min(min, height[s, f]); max = Mathf.Max(max, height[s, f]); }
            float limit = min + (max - min) * 0.1f;
            for (int f = 0; f < frames; f++)
            {
                if (height[s, f] > limit || height[s, f + 1] > limit) continue;
                sum += (forward[s, f] - forward[s, f + 1]) * fps;
                count++;
            }
        }
        return count > 0 ? Mathf.Max(0f, sum / count) : 0f;
    }
}
