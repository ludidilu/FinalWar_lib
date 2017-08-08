﻿using bt;
using System.Collections.Generic;
using System.Xml;
using System;

namespace FinalWar
{
    internal class GetCanSupportHeroPosConditionNode : ConditionNode<Battle, Hero, AiData>
    {
        public int weight;

        public override bool Enter(Battle _t, Hero _u, AiData _v)
        {
            List<int> posList = HeroAi.GetCanSupportHeroPos(_t, _u);

            if (posList != null)
            {
                Dictionary<int, int> dic = new Dictionary<int, int>();

                for (int i = 0; i < posList.Count; i++)
                {
                    _v.Add(posList[i], weight);
                }
            }

            return true;
        }

        internal GetCanSupportHeroPosConditionNode(XmlNode _node)
        {
            XmlAttribute weightAtt = _node.Attributes["weight"];

            if (weightAtt == null)
            {
                throw new Exception("GetCanSupportHeroPosConditionNode has not weight attribute:" + _node.ToString());
            }

            weight = int.Parse(weightAtt.InnerText);
        }
    }
}