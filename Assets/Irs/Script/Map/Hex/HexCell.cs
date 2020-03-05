using System;
using UnityEngine;
using System.Collections.Generic;

namespace IRS.Map
{
    public class HexCell : MonoBehaviour
    {
        public HexGridChunk chunk;

        public HexCoordinates coordinates;

        public Color Color {
            get {
                return color;
            }
            set {
                if (color == value) {
                    return;
                }
                color = value;
                Refresh();
            }
        }

        public float StreamBedY
        {
            get { return elevation + HexMetrics.elevationStep * HexMetrics.streamBedElevationOffset; }
        }

        public RectTransform uiRect;

        public int Elevation {
            get {
                return elevation;
            }
            set {
                elevation = value;
                Vector3 position = transform.localPosition;
                position.y = value * HexMetrics.elevationStep;
                position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
                transform.localPosition = position;

                Vector3 uiPosition = uiRect.localPosition;
                uiPosition.z = -position.y;
                uiRect.localPosition = uiPosition;

                if (elevation == value) {
                    return;
                }

                if (hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation)
                {
                    RemoveOutgoingRiver();
                }

                if (hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation)
                {
                    RemoveIncomingRiver();
                }

                Refresh();
            }
        }

        public Vector3 Position {
            get {
                return transform.localPosition;
            }
        }

        public bool HasIncomingRiver
        {
            get => hasIncomingRiver;
        }

        public bool HasOutgoingRiver
        {
            get => hasOutgoingRiver;
        }

        public HexDirection IncomingRiver
        {
            get { return incomingRiver; }
        }

        public HexDirection OutgoingRiver
        {
            get { return outgoingRiver; }
        }

        public bool HasRiver
        {
            get
            {
                return hasIncomingRiver || hasOutgoingRiver;
            }
        }

        public bool HasRiverBeginOrEnd {
            get {
                return hasIncomingRiver != hasOutgoingRiver;
            }
        }

        private Color color;

        private int elevation = int.MinValue;

        [SerializeField] private HexCell[] neighbors;
        [SerializeField] private bool hasIncomingRiver;
        [SerializeField] private bool hasOutgoingRiver;
        [SerializeField] private HexDirection incomingRiver;
        [SerializeField] private HexDirection outgoingRiver;


        private void Awake()
        {
            neighbors = new HexCell[6];
        }

        public HexCell GetNeighbor(HexDirection direction)
        {
            return neighbors[(int)direction];
        }

        public void SetNeighbor(HexDirection direction, HexCell cell)
        {
            neighbors[(int) direction] = cell;
            cell.neighbors[(int) direction.Opposite()] = this;
        }

        public HexEdgeType GetEdgeType (HexDirection direction) {
            return HexMetrics.GetEdgeType(
                elevation, neighbors[(int)direction].elevation
            );
        }

        public HexEdgeType GetEdgeType (HexCell otherCell) {
            return HexMetrics.GetEdgeType(
                elevation, otherCell.elevation
            );
        }

        public bool HasRiverThroughEdge(HexDirection direction)
        {
            return hasIncomingRiver && incomingRiver == direction ||
                   hasOutgoingRiver && outgoingRiver == direction;
        }

        public void RemoveIncomingRiver()
        {
            if (hasIncomingRiver)
            {
                hasIncomingRiver = false;
                RefreshSelf();

                HexCell neighbor = GetNeighbor(incomingRiver);
                neighbor.hasOutgoingRiver = false;
                neighbor.RefreshSelf();
            }
        }

        public void RemoveOutgoingRiver()
        {
            if (hasOutgoingRiver)
            {
                hasOutgoingRiver = false;
                RefreshSelf();

                HexCell neighbor = GetNeighbor(outgoingRiver);
                neighbor.hasIncomingRiver = false;
                neighbor.RefreshSelf();
            }

        }

        public void RemoveRiver()
        {
            RemoveIncomingRiver();
            RemoveOutgoingRiver();
        }

        public void SetOutgoingRiver(HexDirection direction)
        {
            if (hasOutgoingRiver && outgoingRiver == direction)
            {
                return;
            }

            HexCell neighbor = GetNeighbor(direction);
            if (!neighbor || elevation < neighbor.elevation)
            {
                return;
            }

            RemoveOutgoingRiver();
            if (hasIncomingRiver && incomingRiver == direction)
            {
                RemoveIncomingRiver();
            }

            hasOutgoingRiver = true;
            outgoingRiver = direction;

            RefreshSelf();

            neighbor.RemoveIncomingRiver();
            neighbor.hasIncomingRiver = true;
            neighbor.incomingRiver = direction.Opposite();
            neighbor.RefreshSelf();
        }

        void Refresh () {
            if (chunk) {
                chunk.Refresh();
                for (int i = 0; i < neighbors.Length; i++) {
                    HexCell neighbor = neighbors[i];
                    if (neighbor != null && neighbor.chunk != chunk) {
                        neighbor.chunk.Refresh();
                    }
                }
            }
        }

        void RefreshSelf()
        {
            if (chunk)
            {
                chunk.Refresh();
            }
        }
    }

}