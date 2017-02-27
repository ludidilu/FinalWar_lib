﻿using bt;
using System.Collections.Generic;

namespace FinalWar
{
    internal class GetCanSupportHeroPosNode : ConditionNode<Battle, Hero, AiData>
    {
        public override bool Enter(Battle _t, Hero _u, AiData _v)
        {
            List<int> posList = _t.GetCanSupportHeroPos(_u);

            if (posList.Count > 0)
            {
                _v.posListDic.Add("canSupportHeroPos", posList);
            }

            return true;
        }
    }
}