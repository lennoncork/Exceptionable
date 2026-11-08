// Verifies Get Target Methods Resolves Getter Setter And Compound Accessors.

class C
{
    public int Value { get; set; }
    void M(C instance)
    {
        var value = instance.Value;
        instance.Value = value;
        instance.Value += 1;
    }
}

