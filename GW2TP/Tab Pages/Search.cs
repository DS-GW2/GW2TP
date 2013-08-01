using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using GW2Miner.Engine;
using GW2Miner.Domain;
using ExpanderApp;

namespace GW2TP
{
    /// <summary>
    /// TODO: Ability to click on a "Crafting Cost" subitem to move to the crafting cost tree view tab for that specific item.
    /// </summary>
    public class Search : DataSource
    {
        private const int SEARCH_RESULT_PAGE_SIZE = 10;

        private TradeWorker trader = new TradeWorker();
        private ISearchTab searchTab;
        private Object classLock = typeof(Search);

        private int offset = 1;
        private bool CBtextChanged = false;

        private ColumnInfo[] columns = { 
            new ColumnInfo("Image", null, null, ""), new ColumnInfo("Name", ColumnInfo.getNameSubItem, ColumnInfo.getName, "name"), 
            new ColumnInfo("Level", ColumnInfo.getLevelSubItem, ColumnInfo.getLevel, "level"), new ColumnInfo("Rarity", ColumnInfo.getRaritySubItem, ColumnInfo.getRarity, "rarity"), 
            new ColumnInfo("Sell", ColumnInfo.getSellSubItem, ColumnInfo.getSell, "price"), 
            new ColumnInfo("Supply", ColumnInfo.getSupplySubItem, ColumnInfo.getSupply, "count"), new ColumnInfo("Buy", ColumnInfo.getBuySubItem, ColumnInfo.getBuy, "buy_price"), 
            new ColumnInfo("Demand", ColumnInfo.getDemandSubItem, ColumnInfo.getDemand, ""), new ColumnInfo("Margin", ColumnInfo.getMarginSubItem, ColumnInfo.getMargin, ""), 
            new ColumnInfo("% Profit", ColumnInfo.getPercentProfitSubItem, ColumnInfo.getPercentProfit, ""), new ColumnInfo("Vendor Sell Price", ColumnInfo.getVendorSellPriceSubItem, ColumnInfo.getVendorSellPrice, ""), 
            new ColumnInfo("Crafting Cost", ColumnInfo.getCraftingCostSubItem, ColumnInfo.getCraftingCostComp, ""), 
            new ColumnInfo("Sell Listings", ColumnInfo.GetSellListingsSubItem, null, ""), new ColumnInfo("Buy Listings", ColumnInfo.GetBuyListingsSubItem, null, "") 
        };

        public Search(ISearchTab mainForm)
        {
            this.searchTab = mainForm;
            this.searchResult = new SearchResult(this.searchTab.SearchListViewLV, this.columns, this, null);
            this.dataPager = new SimpleDataPager(SEARCH_RESULT_PAGE_SIZE, this.searchResult, this.searchTab.SearchStatusStripSS);
            this.Init();
        }

        protected override void Init()
        {
            this.searchTab.SearchCB.TextChanged += SearchComboBox_TextChanged; // hook up search combobox dropdown list
            this.searchTab.SearchCB.KeyDown += SearchCB_KeyDown;

            // setup search subcategory to depend on category
            this.searchTab.SearchCategoryCB.SelectedValueChanged += SearchCategoryComboBox_SelectedValueChanged;
            this.searchTab.SearchSubcategoryCB.SelectedValueChanged += SearchSubcategoryCB_SelectedValueChanged;
            this.searchTab.SearchRarityCB.SelectedValueChanged += SearchRarityCB_SelectedValueChanged;
            this.searchTab.SearchArmorWeightCB.SelectedValueChanged += SearchArmorWeightCB_SelectedValueChanged;

            this.searchTab.SearchBtn.Click += SearchButton_Click; // hook up search button

            // Setup search expander
            this.searchTab.SearchExpanderEx.StateChanged += SearchExpander_StateChanged;
            this.searchTab.SearchExpanderEx.BorderStyle = BorderStyle.FixedSingle;
            ExpanderHelper.CreateLabelHeader(this.searchTab.SearchExpanderEx, "Show Filters", SystemColors.ActiveBorder, Properties.Resources.Collapse, Properties.Resources.Expand);

            // Populate search filter comboboxes
            this.searchTab.SearchArmorWeightCB.DataSource = Enum.GetValues(typeof(GW2DBArmorWeightType));
            this.searchTab.SearchCategoryCB.DataSource = Enum.GetValues(typeof(TypeEnum))
                                            .Cast<TypeEnum>()
                                            .OrderBy(x => x.ToString())
                                            .Select(e => e.ToString().Replace("_", " ")).ToList();
            this.searchTab.SearchRarityCB.DataSource = Enum.GetValues(typeof(RarityEnum));

            ResetSearchComboBox(); // reset search comboboxes.  This call can only be after hooking up the Category comboBox SelectedValueChanged has been hooked up.
            this.searchTab.SearchExpanderEx.Collapse();

            base.Init();
        }

        private void SearchRarityCB_SelectedValueChanged(object sender, EventArgs e)
        {
            OnSearchCriteriaChanged();
        }

        private void SearchSubcategoryCB_SelectedValueChanged(object sender, EventArgs e)
        {
            OnSearchCriteriaChanged();
        }

        private void SearchArmorWeightCB_SelectedValueChanged(object sender, EventArgs e)
        {
            GW2DBArmorWeightType selectedType;
            Enum.TryParse<GW2DBArmorWeightType>(this.searchTab.SearchArmorWeightCB.SelectedValue.ToString().Replace(" ", "_"), out selectedType);
            if (selectedType != GW2DBArmorWeightType.All)
            {
                this.getAllPages = true;
            }
            else
            {
                OnSearchCriteriaChanged();
            }
        }

        private void SearchExpander_StateChanged(object sender, EventArgs e)
        {
            Expander expander = (Expander)sender;

            this.searchTab.SearchListViewLV.Top = expander.Bottom + 10;
            this.searchTab.SearchListViewLV.Size = new Size(this.searchTab.SearchListViewLV.Width, expander.ParentForm.Height - this.searchTab.SearchListViewLV.Top - 100);
        }

        private void ResetSearchComboBox()
        {
            //var p = Enum.GetValues(typeof(TypeEnum))
            //                            .Cast<TypeEnum>()
            //                            .Select(e => new
            //                            {
            //                                Value = e,
            //                                Text = e.ToString().Replace("_", " ")
            //                            });
            //this.SearchCategoryComboBox.DataSource = p.Select(e => e.Text).ToList();
            //this.SearchCategoryComboBox.SelectedItem = TypeEnum.Upgrade_Component.ToString().Replace("_", " ");
            this.searchTab.SearchCB.Text = String.Empty;
            this.searchTab.SearchMinLevelTB = Globals.MINLEVEL.ToString();
            this.searchTab.SearchMaxLevelTB = Globals.MAXLEVEL.ToString();
            this.searchTab.SearchCategoryCB.SelectedItem = TypeEnum.All.ToString();  // also automatically setups the subcategory comboBox due to the Category comboBox SelectedValueChanged hook
            this.searchTab.SearchRarityCB.SelectedItem = RarityEnum.All;
            this.searchTab.SearchArmorWeightCB.SelectedItem = GW2DBArmorWeightType.All;
        }

        /// <summary>
        /// Populate the Subcategory comboBox appropriately based on the selection of the Category comboBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchCategoryComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            this.searchTab.SearchArmorWeightCB.SelectedItem = GW2DBArmorWeightType.All;
            OnSearchCriteriaChanged();

            TypeEnum selectedType;
            Enum.TryParse<TypeEnum>(this.searchTab.SearchCategoryCB.SelectedValue.ToString().Replace(" ", "_"), out selectedType);
            switch (selectedType)
            {
                case TypeEnum.Armor:
                    this.searchTab.SearchSubcategoryCB.DataSource = Enum.GetValues(typeof(ArmorSubTypeEnum))
                                                                    .Cast<ArmorSubTypeEnum>()
                                                                    .OrderBy(x => x.ToString())
                                                                    .Select(x => x.ToString().Replace("_", " ")).ToList();
                    this.searchTab.SearchSubcategoryCB.SelectedItem = ArmorSubTypeEnum.All.ToString();
                    break;

                case TypeEnum.Consumable:
                    this.searchTab.SearchSubcategoryCB.DataSource = Enum.GetValues(typeof(ConsumableSubTypeEnum))
                                                                    .Cast<ConsumableSubTypeEnum>()
                                                                    .OrderBy(x => x.ToString())
                                                                    .Select(x => x.ToString().Replace("_", " ")).ToList();
                    this.searchTab.SearchSubcategoryCB.SelectedItem = ConsumableSubTypeEnum.All.ToString();
                    break;

                case TypeEnum.Container:
                    this.searchTab.SearchSubcategoryCB.DataSource = Enum.GetValues(typeof(ContainerSubTypeEnum))
                                                                    .Cast<ContainerSubTypeEnum>()
                                                                    .OrderBy(x => x.ToString())
                                                                    .Select(x => x.ToString().Replace("_", " ")).ToList();
                    this.searchTab.SearchSubcategoryCB.SelectedItem = ContainerSubTypeEnum.All.ToString();
                    break;

                case TypeEnum.Gathering:
                    this.searchTab.SearchSubcategoryCB.DataSource = Enum.GetValues(typeof(GatheringSubTypeEnum))
                                                                    .Cast<GatheringSubTypeEnum>()
                                                                    .OrderBy(x => x.ToString())
                                                                    .Select(x => x.ToString().Replace("_", " ")).ToList();
                    this.searchTab.SearchSubcategoryCB.SelectedItem = GatheringSubTypeEnum.All.ToString();
                    break;

                case TypeEnum.Gizmo:
                    this.searchTab.SearchSubcategoryCB.DataSource = Enum.GetValues(typeof(GizmoSubTypeEnum))
                                                                    .Cast<GizmoSubTypeEnum>()
                                                                    .OrderBy(x => x.ToString())
                                                                    .Select(x => x.ToString().Replace("_", " ")).ToList();
                    this.searchTab.SearchSubcategoryCB.SelectedItem = GizmoSubTypeEnum.All.ToString();
                    break;

                case TypeEnum.Tool:
                    this.searchTab.SearchSubcategoryCB.DataSource = Enum.GetValues(typeof(ToolSubTypeEnum))
                                                                    .Cast<ToolSubTypeEnum>()
                                                                    .OrderBy(x => x.ToString())
                                                                    .Select(x => x.ToString().Replace("_", " ")).ToList();
                    this.searchTab.SearchSubcategoryCB.SelectedItem = ToolSubTypeEnum.All.ToString();
                    break;

                case TypeEnum.Trinket:
                    this.searchTab.SearchSubcategoryCB.DataSource = Enum.GetValues(typeof(TrinketSubTypeEnum))
                                                                    .Cast<TrinketSubTypeEnum>()
                                                                    .OrderBy(x => x.ToString())
                                                                    .Select(x => x.ToString().Replace("_", " ")).ToList();
                    this.searchTab.SearchSubcategoryCB.SelectedItem = TrinketSubTypeEnum.All.ToString();
                    break;

                case TypeEnum.Upgrade_Component:
                    this.searchTab.SearchSubcategoryCB.DataSource = Enum.GetValues(typeof(UpgradeComponentSubTypeEnum))
                                                                    .Cast<UpgradeComponentSubTypeEnum>()
                                                                    .OrderBy(x => x.ToString())
                                                                    .Select(x => x.ToString().Replace("_", " ")).ToList();
                    this.searchTab.SearchSubcategoryCB.SelectedItem = UpgradeComponentSubTypeEnum.All.ToString();
                    break;

                case TypeEnum.Weapon:
                    this.searchTab.SearchSubcategoryCB.DataSource = Enum.GetValues(typeof(WeaponSubTypeEnum))
                                                                    .Cast<WeaponSubTypeEnum>()    
                                                                    .OrderBy(x => x.ToString())
                                                                    .Select(x => x.ToString().Replace("_", " ")).ToList();
                    this.searchTab.SearchSubcategoryCB.SelectedItem = WeaponSubTypeEnum.All.ToString();
                    break;

                default:
                    this.searchTab.SearchSubcategoryCB.DataSource = null;
                    break;
            }

            if ((selectedType == TypeEnum.Armor) || (selectedType == TypeEnum.All))
            {
                this.searchTab.SearchArmorWeightCB.Enabled = true;
            }
            else
            {
                this.searchTab.SearchArmorWeightCB.Enabled = false;
            }
        }

        private void SearchCB_KeyDown(object sender, KeyEventArgs e)
        {
            ComboBox control = (ComboBox)sender;
            if ((e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return) && CBtextChanged)
            {
                e.Handled = true;
                control.Invoke((MethodInvoker)(() => { if (control.Items.Count > 0) control.DroppedDown = false; }));
                CBtextChanged = false;
                this.getAllPages = false;
                this.searchResult.ResetSortOrder();
                this.searchTab.SearchBtn.PerformClick();
            }
        }

        private void OnSearchCriteriaChanged()
        {
            CBtextChanged = true;
            if (SearchArmorWeight == GW2DBArmorWeightType.All) this.getAllPages = false;
            this.searchResult.ResetSortOrder();
            //this.searchTab.SearchArmorWeightCB.SelectedItem = GW2DBArmorWeightType.All;
        }

        private void SearchComboBox_TextChanged(object sender, EventArgs e)
        {
            lock (classLock)
            {
                CBtextChanged = true;
                ComboBox control = (ComboBox)sender;
                string text = control.Text;
                control.DroppedDown = true;

                if (text.Length > Globals.MINCHAR && control.FindString(text) < 0 && !Globals.gettingSessionKey)
                {
                    try
                    {
                        int minLevel, maxLevel;
                        TypeEnum category;
                        int subCategory;
                        RarityEnum rarity;

                        parseSearchFilters(out category, out subCategory, out rarity, out minLevel, out maxLevel);

                        ThreadPool.QueueUserWorkItem((obj) =>
                        {
                            List<Item> itemList = trader.get_search_typeahead(text, category, subCategory, rarity, minLevel, maxLevel).Result;
                            control.Invoke((MethodInvoker)(() =>
                            {
                                control.BeginUpdate();
                                control.Items.Clear();
                                control.SelectionStart = control.Text.Length + 1;
                                List<string> list = itemList.Select(i => i.Name).Distinct().ToList();
                                foreach (string item in list)
                                {
                                    control.Items.Add(item);
                                }

                                control.EndUpdate();
                            }));
                        });
                    }
                    catch
                    {
                        // swallow
                    }
                }
            }
        }

        private TypeEnum SearchCategory
        {
            get
            {
                TypeEnum t;
                Enum.TryParse<TypeEnum>(this.searchTab.SearchCategoryCB.SelectedValue.ToString().Replace(" ", "_"), out t);
                return t;
            }
        }

        private int SearchSubCategory
        {
            get
            {
                TypeEnum category = this.SearchCategory;
                switch (category)
                {
                    case TypeEnum.Armor:
                        ArmorSubTypeEnum subCategoryArmor;
                        Enum.TryParse<ArmorSubTypeEnum>(this.searchTab.SearchSubcategoryCB.SelectedValue.ToString().Replace(" ", "_"), out subCategoryArmor);
                        return (int)subCategoryArmor;

                    case TypeEnum.Consumable:
                        ConsumableSubTypeEnum subCategoryConsumable;
                        Enum.TryParse<ConsumableSubTypeEnum>(this.searchTab.SearchSubcategoryCB.SelectedValue.ToString().Replace(" ", "_"), out subCategoryConsumable);
                        return (int)subCategoryConsumable;

                    case TypeEnum.Container:
                        ContainerSubTypeEnum subCategoryContainer;
                        Enum.TryParse<ContainerSubTypeEnum>(this.searchTab.SearchSubcategoryCB.SelectedValue.ToString().Replace(" ", "_"), out subCategoryContainer);
                        return (int)subCategoryContainer;

                    case TypeEnum.Gathering:
                        GatheringSubTypeEnum subCategoryGathering;
                        Enum.TryParse<GatheringSubTypeEnum>(this.searchTab.SearchSubcategoryCB.SelectedValue.ToString().Replace(" ", "_"), out subCategoryGathering);
                        return (int)subCategoryGathering;

                    case TypeEnum.Gizmo:
                        GizmoSubTypeEnum subCategoryGizmo;
                        Enum.TryParse<GizmoSubTypeEnum>(this.searchTab.SearchSubcategoryCB.SelectedValue.ToString().Replace(" ", "_"), out subCategoryGizmo);
                        return (int)subCategoryGizmo;

                    case TypeEnum.Tool:
                        ToolSubTypeEnum subCategoryTool;
                        Enum.TryParse<ToolSubTypeEnum>(this.searchTab.SearchSubcategoryCB.SelectedValue.ToString().Replace(" ", "_"), out subCategoryTool);
                        return (int)subCategoryTool;

                    case TypeEnum.Trinket:
                        TrinketSubTypeEnum subCategoryTrinket;
                        Enum.TryParse<TrinketSubTypeEnum>(this.searchTab.SearchSubcategoryCB.SelectedValue.ToString().Replace(" ", "_"), out subCategoryTrinket);
                        return (int)subCategoryTrinket;

                    case TypeEnum.Upgrade_Component:
                        UpgradeComponentSubTypeEnum subCategoryUpComponent;
                        Enum.TryParse<UpgradeComponentSubTypeEnum>(this.searchTab.SearchSubcategoryCB.SelectedValue.ToString().Replace(" ", "_"), out subCategoryUpComponent);
                        return (int)subCategoryUpComponent;

                    case TypeEnum.Weapon:
                        WeaponSubTypeEnum subCategoryWeapon;
                        Enum.TryParse<WeaponSubTypeEnum>(this.searchTab.SearchSubcategoryCB.SelectedValue.ToString().Replace(" ", "_"), out subCategoryWeapon);
                        return (int)subCategoryWeapon;

                    default:
                        return -1;
                }
            }
        }

        private RarityEnum SearchRarity
        {
            get
            {
                RarityEnum r;
                Enum.TryParse<RarityEnum>(this.searchTab.SearchRarityCB.SelectedValue.ToString(), out r);
                return r;
            }
        }

        private GW2DBArmorWeightType SearchArmorWeight
        {
            get
            {
                GW2DBArmorWeightType w;
                Enum.TryParse<GW2DBArmorWeightType>(this.searchTab.SearchArmorWeightCB.SelectedValue.ToString(), out w);
                return w;
            }
        }

        private void parseSearchFilters(out TypeEnum category, out int subCategory, out RarityEnum rarity, out int minLevel, out int maxLevel)
        {
            if (!Int32.TryParse(this.searchTab.SearchMinLevelTB, out minLevel)) minLevel = 0;
            if (!Int32.TryParse(this.searchTab.SearchMaxLevelTB, out maxLevel)) maxLevel = 80;

            category = this.SearchCategory;
            subCategory = this.SearchSubCategory;
            rarity = this.SearchRarity;
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            //ThreadPool.QueueUserWorkItem((obj) => fetchData(1, SEARCH_RESULT_PAGE_SIZE, this.searchResult.Update));
            this.cachedResult = null; // Force refresh!
            this.dataPager.Reset();
            this.dataPager.FirstPage();            
        }

        private void InternalFilter(TypeEnum category, GW2DBArmorWeightType weight)
        {
            if (weight == GW2DBArmorWeightType.All) return;

            if (category == TypeEnum.All || category == TypeEnum.Armor)
            {
                this.cachedResult.RemoveAll(x => (x.TypeId == TypeEnum.Armor && x.ArmorWeightType != weight));
                this.totalItems = this.cachedResult.Count;
            }
        }

        public override void fetchData(int offset, int count, DataReadyFunc OnDataReady)
        {
            this.searchTab.SearchListViewLV.Invoke((MethodInvoker)(() =>
            {
                lock (classLock)
                {
                    string text = this.searchTab.SearchCB.Text;

                    int minLevel, maxLevel;
                    TypeEnum category;
                    int subCategory;
                    RarityEnum rarity;

                    this.offset = offset;

                    if (this.cachedResult != null && this.totalItems == this.cachedResult.Count && this.getAllPages)
                    {
                        OnDataReady(this.cachedResult, this.offset, count, this.totalItems, false);
                    }
                    else if (!Globals.gettingSessionKey)
                    {
                        GW2DBArmorWeightType weight = this.SearchArmorWeight;
                        parseSearchFilters(out category, out subCategory, out rarity, out minLevel, out maxLevel);

                        if (getAllPages) offset = 1; // if we are getting all pages we start at offset 1
                        trader.search_items(text, getAllPages,
                                                category,
                                                subCategory,
                                                rarity,
                                                minLevel,
                                                maxLevel, true, offset, count, orderBy, sortDescending).ContinueWith((fetchDataTask) =>
                                                                                                                    {
                                                                                                                        TradeWorker.Args searchArgs = trader.LastSearchArgs;
                                                                                                                        this.totalItems = searchArgs.max;
                                                                                                                        //offset = searchArgs.offset;
                                                                                                                        //count = searchArgs.count;
                                                                                                                        fetchDataTask.Result.ForEach(x => trader.add_GW2DB_data(x));
                                                                                                                        this.cachedResult = fetchDataTask.Result;

                                                                                                                        if (getAllPages)
                                                                                                                        {
                                                                                                                            InternalFilter(category, weight);
                                                                                                                            foreach (Item item in this.cachedResult)
                                                                                                                            {
                                                                                                // If we are getting all pages we should calculate all the recipe costs
                                                                                                // now.  Otherwise we will hang with threading issues if we access 
                                                                                                // them during sort.
                                                                                                                                foreach (gw2dbRecipe recipe in item.Recipes)
                                                                                                                                {
                                                                                                                                    trader.MinAcquisitionCost(recipe);
                                                                                                                                }
                                                                                                                             }
                                                                                                                        }
                                                                                                                        
                                                                                                                        OnDataReady(this.cachedResult, this.offset, count, this.totalItems, orderBy == string.Empty);
                                                                                                                    });
                    }
                }
            }));
        }
    }
}
