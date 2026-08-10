using System;

// 인챈트 한 칸: 속성 + 형태.
[Serializable]
public struct Enchantment : IEquatable<Enchantment>
{
    public EnchantmentId attribute;
    public EnchantmentForm form;

    public Enchantment(EnchantmentId attribute, EnchantmentForm form = EnchantmentForm.None)
    {
        this.attribute = attribute;
        this.form = form;
    }

    public bool Equals(Enchantment other)
    {
        return attribute == other.attribute && form == other.form;
    }

    public override bool Equals(object obj)
    {
        return obj is Enchantment other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)attribute * 397) ^ (int)form;
        }
    }

    public static bool operator ==(Enchantment left, Enchantment right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Enchantment left, Enchantment right)
    {
        return !left.Equals(right);
    }
}
