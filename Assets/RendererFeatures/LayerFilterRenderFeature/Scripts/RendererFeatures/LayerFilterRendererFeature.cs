using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatDarkGame.RendererFeature
{
    /// <summary>
    /// 특정 Layer의 오브젝트를 별도 버퍼에 렌더링하여 후처리 적용하는 렌더피처
    ///     1. Prepass 버퍼에 특정 Layer 오브젝트를 렌더링한다. 
    ///         전용 쉐이더가 필요하며 해당 쉐이더의 1번 Pass를 렌더링
    ///     2. Copypass에서 Prepass 버퍼에 대한 화면 후처리를 적용한다. (Downsampling Blur)
    ///     3. Drawpass에서 특정 Layer 오브젝트 2번 Pass를 렌더링한다. 해당 Pass에서는 Copypass에서 생성한 후처리 버퍼를 샘플링할 수 있다.
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

        // RendererFeature 클래스 생성자 역할 함수. 아래 이벤트 발생시 호출됨.
        /*
         * - 렌더러 기능이 처음 로드될 때,
         * - 렌더러 기능을 활성화 또는 비활성화 할 때,
         * - 렌더러 기능의 인스펙터에서 프로퍼티를 변경한 경우
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

        // 소멸자
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_copypass != null) _copypass.Destroy();
            _copypass = null;
            _prepass = null;
            _drawpass = null;
            _copycolorpass = null;
        }

        // 매 프레임 호출 (카메라)
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            RenderTargetHandle prepassBufferRT = new RenderTargetHandle();      // PrepassBufferRT ID 생성
            
            _prepass.Setup(ref prepassBufferRT, renderer.cameraColorTarget);
            _copypass.Setup(ref prepassBufferRT, _blurIteration, _blurOffset);
            if (_settings.useCopyColorPass) _copycolorpass.Setup(renderer.cameraColorTarget, prepassBufferRT);

            // Pass 호출 (passEvent 순서가 동일하면 호출 순으로 먼저 렌더링됨)
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
                    // Packages 폴더 내부에 존재하는 경우 Resources 예약 폴더가 작동하지 않아 아래와 같이 리소스 참조
                    _shader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.CatDarkGame.LayerFilterRenderFeature/Shaders/RendererFeature/Resources/LayerFilterBlurRT.shader");
                }
            }
#endif
            if(!_shader) Debug.LogError("쉐이더 로드 실패");
        }
    }
}