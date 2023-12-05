using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatDarkGame.RendererFeature
{
    /// <summary>
    /// Ư�� Layer�� ������Ʈ�� ���� ���ۿ� �������Ͽ� ��ó�� �����ϴ� ������ó
    ///     1. Prepass ���ۿ� Ư�� Layer ������Ʈ�� �������Ѵ�. 
    ///         ���� ���̴��� �ʿ��ϸ� �ش� ���̴��� 1�� Pass�� ������
    ///     2. Copypass���� Prepass ���ۿ� ���� ȭ�� ��ó���� �����Ѵ�. (Downsampling Blur)
    ///     3. Drawpass���� Ư�� Layer ������Ʈ 2�� Pass�� �������Ѵ�. �ش� Pass������ Copypass���� ������ ��ó�� ���۸� ���ø��� �� �ִ�.
    /// </summary>
    public class LayerFilterRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Header("Pass Settings")]
            public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
            public bool useCopyColorPass = false;

            [Header("Target Object Settings")]
            public LayerMask layerMask;

            public string shaderTag_Prepass = "SpriteRenderPrepass";
            public ShaderTagId GetShaderTagID_Prepass => new ShaderTagId(shaderTag_Prepass);
            public string shaderTag_Drawpass = "SpriteRenderDrawpass";
            public ShaderTagId GetShaderTagID_Drawpass => new ShaderTagId(shaderTag_Drawpass);
        }

        [SerializeField] private Settings _settings = new Settings();
        [SerializeField] private Shader _shader;

        [Header("Blur Settings")]
        [SerializeField][Range(1, 5)] private int _blurIteration = 3;
        [SerializeField][Range(0.1f, 3.0f)] private float _blurOffset = 1.0f;

        private LayerFilterRendererPass_CopyColor _copycolorpass = null;
        private LayerFilterRendererPass_Prepass _prepass = null;
        private LayerFilterRendererPass_Copy _copypass = null;
        private LayerFilterRendererPass_Drawpass _drawpass = null;

        // RendererFeature Ŭ���� ������ ���� �Լ�. �Ʒ� �̺�Ʈ �߻��� ȣ���.
        /*
         * - ������ ����� ó�� �ε�� ��,
         * - ������ ����� Ȱ��ȭ �Ǵ� ��Ȱ��ȭ �� ��,
         * - ������ ����� �ν����Ϳ��� ������Ƽ�� ������ ���
         */ 
        public override void Create()
        {
            if (_settings == null) return;

            Init_Shader();

            if(_settings.useCopyColorPass) _copycolorpass = new LayerFilterRendererPass_CopyColor(_settings.passEvent + 0);
            _prepass = new LayerFilterRendererPass_Prepass(_settings.passEvent + 0, _settings.layerMask, _settings.GetShaderTagID_Prepass, !_settings.useCopyColorPass);
            _copypass = new LayerFilterRendererPass_Copy(_settings.passEvent + 1, _shader);
            _drawpass = new LayerFilterRendererPass_Drawpass(_settings.passEvent + 2, _settings.layerMask, _settings.GetShaderTagID_Drawpass);
        }

        // �Ҹ���
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_copypass != null) _copypass.Destroy();
            _copypass = null;
            _prepass = null;
            _drawpass = null;
            _copycolorpass = null;
        }

        // �� ������ ȣ�� (ī�޶�)
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            RenderTargetHandle prepassBufferRT = new RenderTargetHandle();      // PrepassBufferRT ID ����
            
            _prepass.Setup(ref prepassBufferRT, renderer.cameraColorTarget);
            _copypass.Setup(ref prepassBufferRT, _blurIteration, _blurOffset);
            if (_settings.useCopyColorPass) _copycolorpass.Setup(renderer.cameraColorTarget, prepassBufferRT);

            // Pass ȣ�� (passEvent ������ �����ϸ� ȣ�� ������ ���� ��������)
            if (_settings.useCopyColorPass) renderer.EnqueuePass(_copycolorpass);
            renderer.EnqueuePass(_prepass);
            renderer.EnqueuePass(_copypass);
            renderer.EnqueuePass(_drawpass);
        }

        private void Init_Shader()
        {
#if UNITY_EDITOR
            if (!_shader)
            {
                _shader = Shader.Find("Hidden/CatDarkGame/LayerFilterRendererFeature/LayerFilterBlurRT");
                if (!_shader)
                {
                    // Packages ���� ���ο� �����ϴ� ��� Resources ���� ������ �۵����� �ʾ� �Ʒ��� ���� ���ҽ� ����
                    _shader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.CatDarkGame.LayerFilterRenderFeature/Shaders/RendererFeature/Resources/LayerFilterBlurRT.shader");
                }
            }
#endif
            if(!_shader) Debug.LogError("���̴� �ε� ����");
        }
    }
}