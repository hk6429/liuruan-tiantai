using System;
using UnityEngine;

namespace Liuruan
{
    public sealed class CharacterBob : MonoBehaviour
    {
        [SerializeField] private Transform motionRoot;
        [SerializeField] private bool autoMeasure = true;
        [SerializeField, Min(.01f)] private float maxSpeed = 8;
        [SerializeField, Min(0)] private float baseFrequency = 1, frequencyPerSpeed = .4f;
        [SerializeField, Min(0)] private float bounce = .06f, sway = 3, lean = 8;
        [SerializeField, Range(0,.015f)] private float breath = .015f;
        [SerializeField, Min(0)] private float breathFrequency = .3f;
        private CharacterBobCore core;
        private Vector3 basePosition, baseScale, previous;
        private Quaternion baseRotation;
        private float externalSpeed;
        private void Awake()
        {
            if (!motionRoot) motionRoot = transform.parent;
            if (!motionRoot || motionRoot == transform || motionRoot.IsChildOf(transform))
            { Debug.LogError("CharacterBob 請掛在模型子物件，Motion Root 指向獨立的移動根物件。", this); enabled = false; return; }
            basePosition = transform.localPosition; baseRotation = transform.localRotation; baseScale = transform.localScale;
            try { core = new CharacterBobCore(maxSpeed, baseFrequency, frequencyPerSpeed, bounce, sway, lean, breath, breathFrequency); }
            catch (ArgumentException e) { Debug.LogError("CharacterBob 參數無效：" + e.Message, this); enabled = false; }
        }
        private void OnEnable() { if (motionRoot) previous = motionRoot.position; }
        public void SetSpeed(float speed)
        {
            HungerCore.RequireFinite(speed, nameof(speed));
            if (speed < 0) throw new ArgumentOutOfRangeException(nameof(speed));
            externalSpeed = speed; autoMeasure = false;
        }
        private void LateUpdate()
        {
            if (core == null || !motionRoot) return;
            Vector3 now = motionRoot.position, delta = now - previous; previous = now;
            float speed = autoMeasure ? (Time.deltaTime > 0 ? new Vector2(delta.x, delta.z).magnitude / Time.deltaTime : 0) : externalSpeed;
            BobPose pose = core.Tick(Time.deltaTime, speed);
            transform.localPosition = basePosition + Vector3.up * pose.Height;
            transform.localRotation = baseRotation * Quaternion.Euler(pose.LeanDegrees, 0, pose.RollDegrees);
            transform.localScale = baseScale * pose.Scale;
        }
        private void OnDisable()
        {
            if (core == null) return;
            transform.localPosition = basePosition; transform.localRotation = baseRotation; transform.localScale = baseScale;
        }
    }
}
