using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MapCreation {
    public class MapData {
        public int size;
        public int[,,] map;
        public void Initialize(int _size) {
            size = _size;
            map = new int[_size, _size, _size];
            for (int i = 0; i < _size; i++) for (int j = 0; j < _size; j++) for (int k = 0; k < _size; k++) map[i, j, k] = 2;
            int middle = _size / 2;
        }
        public int getType(Vector3Int v) {
            return map[v.x,v.y,v.z];
        }
        public void setType(Vector3Int v, int type) {
            map[v.x, v.y, v.z] = type;
        }
    }
    public class SelectionData {
        GameObject begin, end;
        public Wall beginWall, endWall;
        public Vector3Int min, max;
        public int dir;
        public GameObject Begin
        {
            get { return begin; }
            set {
                begin = value;
                beginWall = begin?.GetComponent<Wall>();
            }
        }
        public GameObject End
        {
            get { return end; }
            set {
                end = value;
                endWall = end?.GetComponent<Wall>();
            }
        }
        public void SpaceUpdate() {
            CreationUtils.element ele = CreationUtils.Element[beginWall.dir];
            min = ele(endWall.pos) > ele(beginWall.pos) ? beginWall.frontPos : beginWall.pos;
            ele = CreationUtils.Element[endWall.dir];
            max = ele(beginWall.pos) > ele(endWall.pos) ? endWall.frontPos : endWall.pos;
            if (min.x > max.x) (min.x, max.x) = (max.x, min.x);
            if (min.y > max.y) (min.y, max.y) = (max.y, min.y);
            if (min.z > max.z) (min.z, max.z) = (max.z, min.z);
        }
        public void SideUpdate()
        {
            min = beginWall.pos;
            max = endWall.pos;
            if (min.x > max.x) (min.x, max.x) = (max.x, min.x);
            if (min.y > max.y) (min.y, max.y) = (max.y, min.y);
            if (min.z > max.z) (min.z, max.z) = (max.z, min.z);
        }
        public bool FlatCheck() {
            if (beginWall.dir != endWall.dir) return false;
            dir = beginWall.dir;
            if ((dir == 1 || dir == 0) && beginWall.pos.x == endWall.pos.x) return true;
            if ((dir == 3 || dir == 2) && beginWall.pos.y == endWall.pos.y) return true;
            if ((dir == 5 || dir == 4) && beginWall.pos.z == endWall.pos.z) return true;
            return false;
        }
        public void tempShift(int dir) {
            min += CreationUtils.dv[dir];
            max += CreationUtils.dv[dir];
        }
    }
}

