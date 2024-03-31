using UnityEngine;
using UnityEngine.UI;

namespace ReuseScroller
{
    public struct TestItem
    {
        public string name;
        public int index;
        public Vector2 size;
    }

    public class TestCell : BaseCell<TestItem>
    {
        public Text label;

        public override void UpdateContent(TestItem item)
        {
            label.text = item.name;
        }
    }
}
