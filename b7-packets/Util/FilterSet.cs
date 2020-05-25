using System;
using System.Collections.Generic;
using System.Linq;

namespace b7.Util
{
    public class Filter<TTarget> : List<FilterRule<TTarget>>
    {
        public Filter() { }

        public bool Check(TTarget target)
        {
            // If there are no rules, include the target
            if (!this.Any()) return true;

            // If any exclusive rule matches, exclude the target
            if (this.Where(rule => rule.IsExclusive).Any(rule => rule.Check(target))) return false;

            // If there are no inclusive rules, include the target
            if (!this.Any(rule => !rule.IsExclusive)) return true;

            // Otherwise include the target if any inclusive rule matches
            return this.Where(rule => !rule.IsExclusive).Any(rule => rule.Check(target));
        }
    }

    public class FilterRule<TTarget>
    {
        public bool IsExclusive { get; }
        private readonly Predicate<TTarget> predicate;

        public FilterRule(Predicate<TTarget> predicate, bool isExclusive = false)
        {
            this.predicate = predicate;
            IsExclusive = isExclusive;
        }

        public bool Check(TTarget target) => predicate(target);
    }
}