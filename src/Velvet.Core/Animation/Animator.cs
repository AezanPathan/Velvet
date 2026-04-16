namespace Velvet.Core.Animation;

using SceneModel = Scene.Scene;
using SceneNode = Scene.SceneNode;

public sealed class Animator
{
    private readonly SceneModel _scene;
    private readonly Dictionary<SceneNode, float[]> _animatedTransforms = new();
    private readonly Dictionary<string, AnimationAction> _actions = new();
    private readonly Dictionary<string, SceneNode> _nodesByName = new();
    private readonly Dictionary<SceneNode, float[]> _originalTransforms = new();

    public Animator(SceneModel scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        _scene = scene;

        BuildNodeIndex(_scene.Roots);
        CacheOriginalTransforms(_scene.Roots);
    }

    public void PlayClip(AnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);

        if (_actions.ContainsKey(clip.Name))
            return;

        _actions[clip.Name] = new AnimationAction(clip);
    }

    public void StopClip(string clipName)
    {
        ArgumentNullException.ThrowIfNull(clipName);
        _actions.Remove(clipName);
    }

    public bool IsPlaying(string clipName)
    {
        ArgumentNullException.ThrowIfNull(clipName);
        return _actions.ContainsKey(clipName);
    }

    public void Update(float deltaTime)
    {
        if (deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));

        foreach (var action in _actions.Values)
            action.Advance(deltaTime);

        RebuildAnimatedTransforms();
        ApplyTransformsToScene();
    }

    public float[]? GetAnimatedTransform(SceneNode node)
    {
        return _animatedTransforms.TryGetValue(node, out var t) ? (float[])t.Clone() : null;
    }


    private void BuildNodeIndex(IReadOnlyList<SceneNode> roots)
    {
        foreach (var root in roots)
            IndexNode(root);
    }

    private void CacheOriginalTransforms(IReadOnlyList<SceneNode> roots)
    {
        foreach (var root in roots)
            CacheNodeTransform(root);
    }

    private void IndexNode(SceneNode node)
    {
        if (node.Name != null)
            _nodesByName[node.Name] = node;

        foreach (var child in node.Children)
            IndexNode(child);
    }

    private void CacheNodeTransform(SceneNode node)
    {
        _originalTransforms[node] = node.LocalTransform;

        foreach (var child in node.Children)
            CacheNodeTransform(child);
    }

    private void RebuildAnimatedTransforms()
    {
        _animatedTransforms.Clear();

        foreach (var action in _actions.Values)
        {
            foreach (var channel in action.Clip.Channels)
            {
                if (!_nodesByName.TryGetValue(channel.TargetNodeName, out var node))
                    continue;

                var evaluated = SamplerEvaluator.Evaluate(channel.Sampler, action.CurrentTime);

                if (!_animatedTransforms.TryGetValue(node, out var transform))
                {
                    transform = _originalTransforms.TryGetValue(node, out var original)
                        ? (float[])original.Clone()
                        : (float[])node.LocalTransform.Clone();
                }

                transform = AnimationApplier.ApplyChannel(channel, evaluated, transform);
                _animatedTransforms[node] = transform;
            }
        }
    }

    private void ApplyTransformsToScene()
    {
        foreach (var (node, original) in _originalTransforms)
        {
            node.SetLocalTransform(
                _animatedTransforms.TryGetValue(node, out var animated)
                    ? animated
                    : original
            );
        }
    }

    private sealed class AnimationAction
    {
        public AnimationClip Clip { get; }
        public float CurrentTime { get; private set; }

        public AnimationAction(AnimationClip clip)
        {
            Clip = clip;
        }

        public void Advance(float deltaTime)
        {
            CurrentTime += deltaTime;

            if (Clip.Duration > 0f && CurrentTime > Clip.Duration)
                CurrentTime %= Clip.Duration;
        }
    }
}