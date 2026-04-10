using System.Collections.Generic;
using SceneModel = Velvet.Core.Scene.Scene;
using SceneNode = Velvet.Core.Scene.SceneNode;

namespace Velvet.Core.Animation;

/// <summary>
/// Orchestrates animation playback for a scene, similar to Three.js AnimationMixer.
/// 
/// The Animator:
/// - Maintains a pool of active animation actions (one per playing clip)
/// - Advances time for each action on Update()
/// - Evaluates samplers and applies results to scene nodes
/// - Does NOT manage memory of the scene itself; caller owns the scene
/// 
/// Architecture:
/// - External caller creates an Animator and passes a Scene
/// - Caller plays/stops clips via PlayClip() and StopClip()
/// - Caller calls Update(deltaTime) each frame
/// - Animator looks up nodes by name and updates their local transforms
/// - Mesh instances are collected from the modified SceneNode hierarchy
/// </summary>
public sealed class Animator
{
    private readonly SceneModel _scene;
    private readonly Dictionary<SceneNode, float[]> _animatedTransforms; // Cache of updated transforms
    private readonly Dictionary<string, AnimationAction> _actions; // Play clip -> action
    private readonly Dictionary<string, SceneNode> _nodesByName; // Fast node lookup
    private readonly Dictionary<SceneNode, float[]> _originalTransforms; // Baseline transforms for reset

    /// <summary>
    /// Creates a new Animator for a given scene.
    /// The scene is not modified; the Animator maintains its own transform cache.
    /// </summary>
    public Animator(SceneModel scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _scene = scene;
        _animatedTransforms = new Dictionary<SceneNode, float[]>();
        _actions = new Dictionary<string, AnimationAction>();
        _nodesByName = new Dictionary<string, SceneNode>();
        _originalTransforms = new Dictionary<SceneNode, float[]>();
        
        // Build node name index
        BuildNodeIndex(_scene.Roots);
        CacheOriginalTransforms(_scene.Roots);
    }

    /// <summary>
    /// Starts playing an animation clip.
    /// If the clip is already playing, this is a no-op.
    /// </summary>
    public void PlayClip(AnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);

        if (_actions.ContainsKey(clip.Name))
        {
            return; // Already playing
        }

        var action = new AnimationAction(clip);
        _actions[clip.Name] = action;
    }

    /// <summary>
    /// Stops playing an animation clip by name.
    /// </summary>
    public void StopClip(string clipName)
    {
        ArgumentNullException.ThrowIfNull(clipName);
        _actions.Remove(clipName);
    }

    /// <summary>
    /// Returns true if the given clip is currently playing.
    /// </summary>
    public bool IsPlaying(string clipName)
    {
        ArgumentNullException.ThrowIfNull(clipName);
        return _actions.ContainsKey(clipName);
    }

    /// <summary>
    /// Advances all playing animations by deltaTime seconds.
    /// Must be called once per frame with the frame's delta time.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (deltaTime < 0f)
        {
            throw new ArgumentException("Delta time must be non-negative.", nameof(deltaTime));
        }

        // Advance each action
        foreach (var action in _actions.Values)
        {
            action.Advance(deltaTime);
        }

        // Rebuild transform cache from active actions
        RebuildAnimatedTransforms();

        // Apply transforms to scene nodes (animated or reset to original)
        ApplyTransformsToScene();
    }

    /// <summary>
    /// Gets the current animated local transform for a node, or null if not animated.
    /// Returns a defensive copy to prevent external mutation.
    /// </summary>
    public float[]? GetAnimatedTransform(SceneNode node)
    {
        _animatedTransforms.TryGetValue(node, out var transform);
        return transform is null ? null : (float[])transform.Clone();
    }

    /// <summary>
    /// Recursively indexes all nodes in the scene by name for fast lookup.
    /// </summary>
    private void BuildNodeIndex(IReadOnlyList<SceneNode> roots)
    {
        foreach (var root in roots)
        {
            IndexNode(root);
        }
    }

    private void CacheOriginalTransforms(IReadOnlyList<SceneNode> roots)
    {
        foreach (var root in roots)
        {
            CacheNodeTransform(root);
        }
    }

    private void IndexNode(SceneNode node)
    {
        if (node.Name != null)
        {
            _nodesByName[node.Name] = node;
        }

        foreach (var child in node.Children)
        {
            IndexNode(child);
        }
    }

    private void CacheNodeTransform(SceneNode node)
    {
        _originalTransforms[node] = node.LocalTransform;

        foreach (var child in node.Children)
        {
            CacheNodeTransform(child);
        }
    }

    /// <summary>
    /// Rebuilds the animated transform cache by evaluating all active channels.
    /// </summary>
    private void RebuildAnimatedTransforms()
    {
        // Clear the cache
        _animatedTransforms.Clear();

        // For each active action, evaluate all its channels and accumulate transforms
        foreach (var action in _actions.Values)
        {
            var clip = action.Clip;

            foreach (var channel in clip.Channels)
            {
                // Look up the target node
                if (!_nodesByName.TryGetValue(channel.TargetNodeName, out var node))
                {
                    // Node not found; skip this channel
                    continue;
                }

                // Evaluate the sampler at the current action time
                var evaluated = SamplerEvaluator.Evaluate(channel.Sampler, action.CurrentTime);

                // Get or create the animated transform for this node
                if (!_animatedTransforms.TryGetValue(node, out var transform))
                {
                    if (_originalTransforms.TryGetValue(node, out var original))
                    {
                        transform = (float[])original.Clone();
                    }
                    else
                    {
                        transform = (float[])node.LocalTransform.Clone();
                    }
                }

                // Apply the channel to the transform
                transform = AnimationApplier.ApplyChannel(channel, evaluated, transform);

                // Update cache
                _animatedTransforms[node] = transform;
            }
        }
    }

    private void ApplyTransformsToScene()
    {
        foreach (var (node, originalTransform) in _originalTransforms)
        {
            if (_animatedTransforms.TryGetValue(node, out var animated))
            {
                node.SetLocalTransform(animated);
            }
            else
            {
                node.SetLocalTransform(originalTransform);
            }
        }
    }

    /// <summary>
    /// Internal action state for a playing animation clip.
    /// </summary>
    private sealed class AnimationAction
    {
        public AnimationClip Clip { get; }
        public float CurrentTime { get; private set; }

        public AnimationAction(AnimationClip clip)
        {
            Clip = clip;
            CurrentTime = 0f;
        }

        public void Advance(float deltaTime)
        {
            CurrentTime += deltaTime;

            // Simple looping: wrap time when it exceeds duration
            if (CurrentTime > Clip.Duration && Clip.Duration > 0f)
            {
                CurrentTime = CurrentTime % Clip.Duration;
            }
        }
    }
}
