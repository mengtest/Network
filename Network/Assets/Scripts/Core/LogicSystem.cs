﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WLGame
{
    public abstract class LogicSystem
    {
        // 是否调用过Start
        bool _invoked = false;

        /// <summary>
        /// Enable机制，用于决定是否需要调用Service的Update
        /// </summary>
        bool _enabled = true;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled == value)
                {
                    return;
                }
                _enabled = value;
                if (_enabled)
                {
                    OnEnable();
                }
                else
                {
                    OnDisable();
                }
            }
        }

        /// <summary>
        /// 初始化/反初始化。
        /// 注：不依赖其它系统的初始化可以放在构造函数中；
        /// 需要依赖其他系统的，必须放在Init中
        /// </summary>
        public virtual void Init() { }
        public virtual void Destory() { }

        public void Update()
        {
            if (!_enabled)
            {
                return;
            }

            if (!_invoked)
            {
                OnStart();
                _invoked = true;
                return;
            }

            OnUpdate();
        }

        public void LateUpdate()
        {
            if (!_enabled)
            {
                return;
            }

            OnLateUpdate();
        }


        // 第一次Update时会调用OnStart()，之后不会再调用
        // Lazy load的部分可以放在OnStart()中
        protected virtual void OnStart() {}

        // 每帧的更新
        protected virtual void OnUpdate() { }
        protected virtual void OnLateUpdate()  { }
        
        // 配合Enable机制
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
    }
}
