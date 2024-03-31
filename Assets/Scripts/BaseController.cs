using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ReuseScroller
{
    // scroll direction
    public enum Direction
    {
        Vertical,
        Horizontal
    }

    /// <summary>
    /// Scroll list control class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [RequireComponent(typeof(RectTransform), typeof(ScrollRect))]
    [DisallowMultipleComponent]
    public abstract class BaseController<T> : UIBehaviour
    {
        public Direction scrollDirection;
        public bool scrollReverse;
        public GameObject cellObject;
        public float defaultCellSize = 200.0f;
        public float spacing = 20.0f;
        public RectOffset contentPadding;
        public float activePadding;

        [Header("Number of rows or columns")]
        public int rowOrColCnt = 2;

        private RectTransform rectTransform;
        private ScrollRect scrollRect;
        private Vector2 scrollPosition;
        private readonly LinkedList<BaseCell<T>> cells = new LinkedList<BaseCell<T>>();

        private List<T> cellData = new List<T>();
        protected List<T> CellData
        {
            get => cellData;
            set
            {
                cellData = value;
                ReloadData(true);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            rectTransform = GetComponent<RectTransform>();
            scrollRect = GetComponent<ScrollRect>();

            RectTransform contentRectTransform = scrollRect.content.GetComponent<RectTransform>();
            if (scrollDirection == Direction.Vertical)
            {
                if (scrollReverse)
                {
                    contentRectTransform.anchorMin = Vector2.zero;
                    contentRectTransform.anchorMax = Vector2.right;
                }
                else
                {
                    contentRectTransform.anchorMin = Vector2.up;
                    contentRectTransform.anchorMax = Vector2.one;
                }
            }
            else if (scrollDirection == Direction.Horizontal)
            {
                if (scrollReverse)
                {
                    contentRectTransform.anchorMin = Vector2.right;
                    contentRectTransform.anchorMax = Vector2.one;
                }
                else
                {
                    contentRectTransform.anchorMin = Vector2.zero;
                    contentRectTransform.anchorMax = Vector2.up;
                }
            }
            contentRectTransform.anchoredPosition = Vector2.zero;
            contentRectTransform.sizeDelta = Vector2.zero;

            scrollRect.onValueChanged.AddListener(OnScrolled);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (cellObject && !cellObject.GetComponent<BaseCell<T>>())
            {
                cellObject = null;
            }
        }

        private void OnScrolled(Vector2 pos)
        {
            ReuseCells(pos - scrollPosition);
            FillCells();
            scrollPosition = pos;
        }

        protected void ReloadData(bool isReset = false)
        {
            Vector2 sizeDelta = scrollRect.content.sizeDelta;
            float contentSize = 0;
            var cnt = Math.Ceiling((double)cellData.Count / rowOrColCnt);
            for (int i = 0; i < cnt; i++)
            {
                contentSize += GetCellSize(i) + (i > 0 ? spacing : 0);
            }
            if (scrollDirection == Direction.Vertical)
            {
                contentSize += contentPadding.vertical;
                sizeDelta.y = contentSize > rectTransform.rect.height ? contentSize : rectTransform.rect.height;
            }
            else if (scrollDirection == Direction.Horizontal)
            {
                contentSize += contentPadding.horizontal;
                sizeDelta.x = contentSize > rectTransform.rect.width ? contentSize : rectTransform.rect.width;
            }
            scrollRect.content.sizeDelta = sizeDelta;

            if (isReset)
            {
                foreach (BaseCell<T> cell in cells)
                {
                    Destroy(cell.gameObject);
                }
                cells.Clear();

                scrollRect.normalizedPosition = scrollRect.content.GetComponent<RectTransform>().anchorMin;
                scrollRect.onValueChanged.Invoke(scrollRect.normalizedPosition);
            }
            else
            {
                UpdateCells();
                FillCells();
            }
        }

        private void CreateCell(int index)
        {
            BaseCell<T> cell = Instantiate(cellObject).GetComponent<BaseCell<T>>();
            cell.SetAnchors(scrollRect.content.anchorMin, scrollRect.content.anchorMax);
            cell.transform.SetParent(scrollRect.content.transform, false);

            UpdateCell(cell, index);

            var corns = GetCorners(index);

            if (scrollDirection == Direction.Vertical)
            {
                if (scrollReverse)
                {
                    cell.Bottom = cells.Count > 0
                        ? (GetRow(index) > GetRow(cells.Last.Value.dataIndex)) ? cells.Last.Value.Top + spacing : cells.Last.Value.Bottom
                        : contentPadding.bottom;
                    cell.SetOffsetHorizontal(corns[1], corns[0]);
                }
                else
                {
                    cell.Top = cells.Count > 0
                        ? (GetRow(index) > GetRow(cells.Last.Value.dataIndex)) ? cells.Last.Value.Bottom - spacing : cells.Last.Value.Top
                        : -contentPadding.top;
                    cell.SetOffsetHorizontal(corns[0], corns[1]);
                }
            }
            else if (scrollDirection == Direction.Horizontal)
            {
                if (scrollReverse)
                {
                    cell.Right = cells.Count > 0
                        ? (GetCol(index) > GetCol(cells.Last.Value.dataIndex)) ? cells.Last.Value.Left - spacing : cells.Last.Value.Right
                        : -contentPadding.right;
                    cell.SetOffsetVertical(corns[3], corns[2]);
                }
                else
                {
                    cell.Left = cells.Count > 0
                        ? (GetCol(index) > GetCol(cells.Last.Value.dataIndex)) ? cells.Last.Value.Right + spacing : cells.Last.Value.Left
                        : contentPadding.left;
                    cell.SetOffsetVertical(corns[2], corns[3]);
                }
            }

            cells.AddLast(cell);
        }

        private float[] GetCorners(int index)
        {
            float left = 0, right = 0;
            float top = 0, bottom = 0;

            var corns = new float[4];

            var width = (rectTransform.sizeDelta.x - (rowOrColCnt + 1) * contentPadding.left) / rowOrColCnt; // 每一个item的宽度
            var height = (rectTransform.sizeDelta.y - (rowOrColCnt + 1) * contentPadding.top) / rowOrColCnt; // 每一个item的宽度

            if (scrollDirection == Direction.Vertical)
            {
                left = contentPadding.left + (width + contentPadding.left) * (index % rowOrColCnt);
                right = contentPadding.right + (width + contentPadding.right) * (rowOrColCnt - index % rowOrColCnt - 1);
            }

            if (scrollDirection == Direction.Horizontal)
            {
                top = contentPadding.top + (height + contentPadding.top) * (index % rowOrColCnt);
                bottom = contentPadding.bottom + (height + contentPadding.bottom) * (rowOrColCnt - index % rowOrColCnt - 1);
            }

            corns[0] = left;
            corns[1] = right;
            corns[2] = top;
            corns[3] = bottom;

            return corns;
        }

        private float GetRow(int index)
        {
            var row = Mathf.Floor(index / rowOrColCnt) + 1;
            return row;
        }

        private double GetCol(int index)
        {
            var col = Mathf.Floor(index / rowOrColCnt) + 1;
            return col;
        }

        private void UpdateCell(BaseCell<T> cell, int index)
        {
            cell.dataIndex = index;
            if (cell.dataIndex >= 0 && cell.dataIndex < cellData.Count)
            {
                if (scrollDirection == Direction.Vertical)
                {
                    cell.Height = GetCellSize(cell.dataIndex);
                }
                else if (scrollDirection == Direction.Horizontal)
                {
                    cell.Width = GetCellSize(cell.dataIndex);
                }
                cell.UpdateContent(cellData[cell.dataIndex]);
                cell.gameObject.SetActive(true);
            }
            else
            {
                cell.gameObject.SetActive(false);
            }
        }

        private void UpdateCells()
        {
            if (cells.Count == 0) return;

            LinkedListNode<BaseCell<T>> node = cells.First;
            UpdateCell(node.Value, node.Value.dataIndex);
            node = node.Next;
            while (node != null)
            {
                UpdateCell(node.Value, node.Previous.Value.dataIndex + 1);

                var corns = GetCorners(node.Value.dataIndex);

                if (scrollDirection == Direction.Vertical)
                {
                    if (scrollReverse)
                    {
                        node.Value.Bottom = (GetRow(node.Value.dataIndex) > GetRow(node.Previous.Value.dataIndex))
                            ? node.Previous.Value.Top + spacing
                            : node.Previous.Value.Bottom;
                        node.Value.SetOffsetHorizontal(corns[1], corns[0]);
                    }
                    else
                    {
                        node.Value.Top = (GetRow(node.Value.dataIndex) > GetRow(node.Previous.Value.dataIndex)) 
                            ? node.Previous.Value.Bottom - spacing
                            : node.Previous.Value.Top;
                        node.Value.SetOffsetHorizontal(corns[0], corns[1]);
                    }
                }
                else if (scrollDirection == Direction.Horizontal)
                {
                    if (scrollReverse)
                    {
                        node.Value.Right = (GetCol(node.Value.dataIndex) > GetCol(node.Previous.Value.dataIndex))
                            ? node.Previous.Value.Left - spacing
                            : node.Previous.Value.Right;
                        node.Value.SetOffsetVertical(corns[3], corns[2]);
                    }
                    else
                    {
                        node.Value.Left = (GetCol(node.Value.dataIndex) > GetCol(node.Previous.Value.dataIndex))
                            ? node.Previous.Value.Right + spacing
                            : node.Previous.Value.Left;
                        node.Value.SetOffsetVertical(corns[2], corns[3]);
                    }
                }

                node = node.Next;
            }
        }

        private void FillCells()
        {
            if (cells.Count == 0)
                CreateCell(0);

            if (cells.Last.Value.dataIndex >= cellData.Count)
                return;

            while (cells.Last.Value.dataIndex < cellData.Count && CellsTailEdge + spacing < ActiveTailEdge)
            {
                CreateCell(cells.Last.Value.dataIndex + 1);
            }
        }

        private void ReuseCells(Vector2 scrollVector)
        {
            if (cells.Count == 0)
                return;

            if (scrollReverse)
                scrollVector *= -1;

            if (scrollDirection == Direction.Vertical)
            {
                if (scrollVector.y > 0) // Slide from top to bottom
                {
                    while (cells.First.Value.dataIndex > 0 && CellsTailEdge - GetCellSize(cells.Last.Value.dataIndex) >= ActiveTailEdge)
                    {
                        MoveCellLastToFirst();
                    }
                }
                else if (scrollVector.y < 0) // Slide from bottom to top
                {
                    while (CellsHeadEdge + GetCellSize(cells.First.Value.dataIndex) <= ActiveHeadEdge)
                    {
                        MoveCellFirstToLast();
                    }
                }
            }
            else if (scrollDirection == Direction.Horizontal)
            {
                if (scrollVector.x > 0) // Slide from right to left
                {
                    while (CellsHeadEdge + GetCellSize(cells.First.Value.dataIndex) <= ActiveHeadEdge)
                    {
                        MoveCellFirstToLast();
                    }
                }
                else if (scrollVector.x < 0) // Slide from left to right
                {
                    while (cells.First.Value.dataIndex > 0 && CellsTailEdge - GetCellSize(cells.Last.Value.dataIndex) >= ActiveTailEdge)
                    {
                        MoveCellLastToFirst();
                    }
                }
            }
        }

        private void MoveCellFirstToLast()
        {
            if (cells.Count == 0) return;

            BaseCell<T> firstCell = cells.First.Value;
            BaseCell<T> lastCell = cells.Last.Value;

            UpdateCell(firstCell, lastCell.dataIndex + 1);

            var corns = GetCorners(firstCell.dataIndex);

            if (scrollDirection == Direction.Vertical)
            {
                if (scrollReverse)
                {
                    firstCell.Bottom = (GetRow(firstCell.dataIndex) > GetRow(lastCell.dataIndex))
                        ? lastCell.Top + spacing
                        : lastCell.Bottom;
                    firstCell.SetOffsetHorizontal(corns[1], corns[0]);
                }
                else
                {
                    firstCell.Top = (GetRow(firstCell.dataIndex) > GetRow(lastCell.dataIndex)) 
                        ? lastCell.Bottom - spacing 
                        : lastCell.Top;
                    firstCell.SetOffsetHorizontal(corns[0], corns[1]);
                }
            }
            else if (scrollDirection == Direction.Horizontal)
            {
                if (scrollReverse)
                {
                    firstCell.Right = (GetCol(firstCell.dataIndex) > GetCol(lastCell.dataIndex))
                        ? lastCell.Left - spacing
                        : lastCell.Right;
                    firstCell.SetOffsetVertical(corns[3], corns[2]);
                }
                else
                {
                    firstCell.Left = (GetCol(firstCell.dataIndex) > GetCol(lastCell.dataIndex))
                        ? lastCell.Right + spacing
                        : lastCell.Left;
                    firstCell.SetOffsetVertical(corns[2], corns[3]);
                }
            }

            cells.RemoveFirst();
            cells.AddLast(firstCell);
        }

        private void MoveCellLastToFirst()
        {
            if (cells.Count == 0)
                return;

            BaseCell<T> lastCell = cells.Last.Value;
            BaseCell<T> firstCell = cells.First.Value;

            if (firstCell.dataIndex <= 0)
                firstCell.dataIndex = 1;

            UpdateCell(lastCell, firstCell.dataIndex - 1);

            var corns = GetCorners(lastCell.dataIndex);

            if (scrollDirection == Direction.Vertical)
            {
                if (scrollReverse)
                {
                    lastCell.Top = (GetRow(firstCell.dataIndex) > GetRow(lastCell.dataIndex))
                        ? firstCell.Bottom - spacing
                        : firstCell.Top;
                    lastCell.SetOffsetHorizontal(corns[1], corns[0]);
                }
                else
                {
                    lastCell.Bottom = (GetRow(firstCell.dataIndex) > GetRow(lastCell.dataIndex))
                        ? firstCell.Top + spacing
                        : firstCell.Bottom;
                    lastCell.SetOffsetHorizontal(corns[0], corns[1]);
                }
            }
            else if (scrollDirection == Direction.Horizontal)
            {
                if (scrollReverse)
                {
                    lastCell.Left = (GetCol(firstCell.dataIndex) > GetCol(lastCell.dataIndex))
                        ? firstCell.Right + spacing
                        : firstCell.Left;
                    lastCell.SetOffsetVertical(corns[3], corns[2]);
                }
                else
                {
                    lastCell.Right = (GetCol(firstCell.dataIndex) > GetCol(lastCell.dataIndex))
                        ? firstCell.Left - spacing
                        : firstCell.Right;
                    lastCell.SetOffsetVertical(corns[2], corns[3]);
                }
            }

            cells.RemoveLast();
            cells.AddFirst(lastCell);
        }

        protected virtual float GetCellSize(int index)
        {
            return defaultCellSize;
        }

        private float ActiveHeadEdge
        {
            get
            {
                float edge = -activePadding;
                if (scrollDirection == Direction.Vertical)
                {
                    if (scrollReverse)
                    {
                        edge += scrollRect.content.rect.height - scrollRect.content.anchoredPosition.y;
                    }
                    else
                    {
                        edge += scrollRect.content.anchoredPosition.y;
                    }
                }
                else if (scrollDirection == Direction.Horizontal)
                {
                    if (scrollReverse)
                    {
                        edge += scrollRect.content.rect.width + scrollRect.content.anchoredPosition.x;
                    }
                    else
                    {
                        edge += -scrollRect.content.anchoredPosition.x;
                    }
                }
                return edge;
            }
        }

        private float ActiveTailEdge
        {
            get
            {
                float edge = activePadding;
                if (scrollDirection == Direction.Vertical)
                {
                    if (scrollReverse)
                    {
                        edge += scrollRect.content.rect.height - scrollRect.content.anchoredPosition.y + rectTransform.rect.height;
                    }
                    else
                    {
                        edge += scrollRect.content.anchoredPosition.y + rectTransform.rect.height; // 已经滑到了最底部
                    }
                }
                else if (scrollDirection == Direction.Horizontal)
                {
                    if (scrollReverse)
                    {
                        edge += scrollRect.content.rect.width + scrollRect.content.anchoredPosition.x + rectTransform.rect.width;
                    }
                    else
                    {
                        edge += -scrollRect.content.anchoredPosition.x + rectTransform.rect.width;
                    }
                }
                return edge;
            }
        }

        private float CellsHeadEdge
        {
            get
            {
                if (scrollDirection == Direction.Vertical)
                {
                    return scrollReverse
                        ? cells.Count > 0 ? cells.First.Value.Bottom : contentPadding.bottom
                        : cells.Count > 0 ? -cells.First.Value.Top : contentPadding.top;
                }
                else if (scrollDirection == Direction.Horizontal)
                {
                    return scrollReverse
                        ? cells.Count > 0 ? -cells.First.Value.Right : contentPadding.right
                        : cells.Count > 0 ? cells.First.Value.Left : contentPadding.left;
                }
                return 0;
            }
        }

        private float CellsTailEdge
        {
            get
            {
                if (scrollDirection == Direction.Vertical)
                {
                    return scrollReverse
                        ? cells.Count > 0 ? cells.Last.Value.Top : contentPadding.top
                        : cells.Count > 0 ? -cells.Last.Value.Bottom : contentPadding.bottom;
                }
                else if (scrollDirection == Direction.Horizontal)
                {
                    return scrollReverse
                        ? cells.Count > 0 ? -cells.Last.Value.Left : contentPadding.left
                        : cells.Count > 0 ? cells.Last.Value.Right : contentPadding.right;
                }
                return 0;
            }
        }
    }
}
