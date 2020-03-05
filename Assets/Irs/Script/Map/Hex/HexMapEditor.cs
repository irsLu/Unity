using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IRS.Map
{
    public class HexMapEditor : MonoBehaviour
    {
        public Color[] colors;
        public HexGrid hexGrid;

        private Color activeColor;
        private int activeElevation;
        private bool applyColor;
        private bool applyElevation = true;
        private int brushSize;

        enum OptionalToggle
        {
            Ignore,
            Yes,
            No,
        }
        private OptionalToggle riverMode;
        private bool isDrag;
        private HexCell previousCell;
        private HexDirection dragDirection;

        void Awake()
        {
            SelectColor(0);
        }

        void Update()
        {
            if (Input.GetMouseButton(0) &&
                !EventSystem.current.IsPointerOverGameObject())
            {
                HandleInput();
            }
            else {
                previousCell = null;
            }
        }

        void HandleInput()
        {
            Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(inputRay, out hit))
            {
                HexCell cell = hexGrid.GetCell(hit.point);
                if (previousCell && previousCell != cell) {
                    ValidateDrag(cell);
                }
                else {
                    isDrag = false;
                }
                EditCells(hexGrid.GetCell(hit.point));
                previousCell = cell;
            }
            else
            {
                previousCell = null;
            }
        }
        void EditCells (HexCell center) {
            int centerX = center.coordinates.X;
            int centerZ = center.coordinates.Z;

            for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) {
                for (int x = centerX - r; x <= centerX + brushSize; x++) {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }
            for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) {
                for (int x = centerX - brushSize; x <= centerX + r; x++) {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }

        }

        void ValidateDrag (HexCell currentCell) {
            for (
                dragDirection = HexDirection.NE;
                dragDirection <= HexDirection.NW;
                dragDirection++
            ) {
                if (previousCell.GetNeighbor(dragDirection) == currentCell) {
                    isDrag = true;
                    return;
                }
            }
            isDrag = false;
        }

        void EditCell (HexCell cell) {
            if (cell) {
                if (applyColor) {
                    cell.Color = activeColor;
                }
                if (applyElevation) {
                    cell.Elevation = activeElevation;
                }
                if (riverMode == OptionalToggle.No) {
                    cell.RemoveRiver();
                }
                else if (isDrag && riverMode == OptionalToggle.Yes) {
                    HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                    if (otherCell) {
                        otherCell.SetOutgoingRiver(dragDirection);
                    }
                }
            }
        }

        public void SelectColor(int index)
        {
            applyColor = index >= 0;
            if (applyColor) {
                activeColor = colors[index];
            }
        }

        public void SetElevation (float elevation) {
            activeElevation = (int)elevation;
        }

        public void SetApplyElevation (bool toggle) {
            applyElevation = toggle;
        }

        public void SetBrushSize (float size) {
            brushSize = (int)size;
        }

        public void ShowUI (bool visible) {
            hexGrid.ShowUI(visible);
        }

        public void SetRiverMode(int mode)
        {
            riverMode = (OptionalToggle)mode;
        }
    }
}