using UnityEngine;
using MasaChuang.SolidText3D;

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
            "立体文字",        // 日本語
            "汉字",            // 中国語
            "한글",            // 韓国語
            "Hello 世界",      // 混在テキスト
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
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _currentIndex = (_currentIndex + 1) % _samples.Length;
                DisplayCurrent();
            }
        }

        private void DisplayCurrent()
        {
            if (_text3D != null)
                _text3D.Text = _samples[_currentIndex];
        }
    }
}
