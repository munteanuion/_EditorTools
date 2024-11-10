#if UNITY_2022_1_OR_NEWER
using UnityEngine.UIElements;

namespace DA_Assets.UEL
{
    public class UitkTreeView : UitkLinker<TreeView> { }

#if UNITY_6000_0_OR_NEWER
    [UxmlElement(nameof(TreeViewG))]
    public partial class TreeViewG : UnityEngine.UIElements.TreeView, IHaveGuid
    {
        [UxmlAttribute]
        public string guid { get; set; }

        public TreeViewG()
        {
            guid = GuidGenerator.GenerateGuid(guid);
        }
    }
#else
    public class TreeViewG : UnityEngine.UIElements.TreeView, IHaveGuid
    {
        public string guid { get; set; }

        public new class UxmlFactory : UxmlFactory<TreeViewG, UxmlTraits> { }

        public new class UxmlTraits : BaseTreeView.UxmlTraits
        {
            UxmlStringAttributeDescription m_Guid = GuidGenerator.GetGuidField();

            public override void Init(UnityEngine.UIElements.VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                TreeViewG obj = ve as TreeViewG;
                GuidGenerator.GenerateGuid(m_Guid, obj, bag, cc);
            }
        }
    }
#endif
}
#endif