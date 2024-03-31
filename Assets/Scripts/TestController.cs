using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ReuseScroller
{
    public class TestController : BaseController<TestItem>
    {
        protected override void Start()
        {
            base.Start();
            var items = new List<TestItem>();
            for (int i = 0; i <= 19; i++)
            {
                items.Add(new TestItem
                {
                    name = i.ToString("d"),
                    index = i,
                    size = new Vector2(100f, 200f),
                });
            }
            CellData = items;
        }

        // Reset scroll item size
        //protected override float GetCellSize(int index)
        //{
        //    return index == 0 ? 100.0f : defaultCellSize;
        //}
    }
}
