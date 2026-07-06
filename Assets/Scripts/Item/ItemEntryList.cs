[System.Serializable]
public class ItemEntryList
{
    public int length;
    public ItemEntry[] entries;

    public void Resize()
    {
        if (entries != null)
        {
            System.Array.Clear(entries, 0, entries.Length);
        }

        entries = new ItemEntry[length];
    }
}
