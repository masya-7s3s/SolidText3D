using UnityEngine;
using MasaChuang.SolidText3D;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MasaChuang.SolidText3D.Samples
{
    /// <summary>
    /// スクリプトから SolidText3DComponent のテキストを動的に変更するサンプル。
    /// SolidText3DComponent がアタッチされた GameObject にこのスクリプトをアタッチしてください。
    /// </summary>
    public class BasicUsageExample : MonoBehaviour
    {
        private SolidText3DComponent _text3D;
        private int _score;

        private void Start()
        {
            _text3D = GetComponent<SolidText3DComponent>();
            if (_text3D == null)
            {
                Debug.LogError("[BasicUsageExample] SolidText3DComponent が見つかりません。");
                return;
            }

            UpdateScoreText();
        }

        private void Update()
        {
            // スペースキーでスコアを増加させてテキストを動的に更新
            if (IsSpacePressedThisFrame())
            {
                _score += 100;
                UpdateScoreText();
            }
        }

        private static bool IsSpacePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        private void UpdateScoreText()
        {
            if (_text3D != null)
            {
                _text3D.Text = $"Score: {_score}";
                _text3D.RegenerateMesh();
            }
        }
    }
}
