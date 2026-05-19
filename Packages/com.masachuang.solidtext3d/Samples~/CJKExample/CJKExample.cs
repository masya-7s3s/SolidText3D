using UnityEngine;
using MasaChuang.SolidText3D;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MasaChuang.SolidText3D.Samples
{
    /// <summary>
    /// 日本語・中国語・韓国語（CJK）テキストを 3D メッシュとして表示するサンプル。
    /// SolidText3DComponent がアタッチされた GameObject にこのスクリプトをアタッチしてください。
    /// Noto Sans JP フォント（デフォルト埋め込み）がすべての CJK 文字をサポートします。
    /// </summary>
    public class CJKExample : MonoBehaviour
    {
        private SolidText3DComponent _text3D;

        private readonly string[] _samples =
        {
            "立体文字",         // 標準
            "𠮷﨑嵒",           // 環境依存
            "+-<>=",           // 記号
            "こんにちは、世界"
        };

        private int _currentIndex;

        private void Start()
        {
            _text3D = GetComponent<SolidText3DComponent>();
            if (_text3D == null)
            {
                Debug.LogError("[CJKExample] SolidText3DComponent が見つかりません。");
                return;
            }

            DisplayCurrent();
        }

        private void Update()
        {
            // スペースキーで次のサンプルテキストに切り替え
            if (IsSpacePressedThisFrame())
            {
                _currentIndex = (_currentIndex + 1) % _samples.Length;
                DisplayCurrent();
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

        private void DisplayCurrent()
        {
            if (_text3D != null)
            {
                _text3D.Text = _samples[_currentIndex];
                _text3D.RegenerateMesh();
            }
        }
    }
}
