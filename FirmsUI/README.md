# Building Management (Firms) Data Model & UI Console

This directory contains the front-end components, data model interfaces, and interactive layout representations for a deep microeconomic building management system ("Firms") modeled after the classic mechanics of *Capitalism Lab*.

## Directory Contents

- [types.ts](file:///C:/Users/grysi/.gemini/antigravity/scratch/TycoonGame/FirmsUI/types.ts): Contains complete TypeScript type definitions and interfaces for the building models, sector-specific sub-structures, and 3x3 functional block layouts.
- [index.html](file:///C:/Users/grysi/.gemini/antigravity/scratch/TycoonGame/FirmsUI/index.html): A standalone, fully interactive HTML5/Tailwind/JS mock dashboard. It showcases all 7 building views (Retail, Manufacturing, Extraction, Farm, Research, Real Estate, and Headquarters) with active controls, sliders, alerts, operations suspension, and real-time canvas-based sparklines.

---

## 1. Data Model Integration (`types.ts`)

The simulation engine represents all properties of a building through the hierarchical `Firm` interface.

```typescript
export interface Firm {
    id: string;
    type: BuildingType;
    name: string;
    x: number;
    y: number;
    isOperational: boolean;
    efficiency: number;       // 0-100%
    upkeepCostPerHour: number;
    revenuePerHour: number;
    netProfitPerHour: number;
    financialHistory: { timestamp: string; revenue: number; profit: number }[];
    
    // Optional structural details based on type
    layoutMatrix?: FunctionalBlock[]; 
    retailDetails?: RetailDetails;
    manufacturingDetails?: ManufacturingDetails;
    extractionDetails?: ExtractionDetails;
    farmDetails?: FarmDetails;
    researchDetails?: ResearchDetails;
    realEstateDetails?: RealEstateDetails;
    hqDetails?: HQDetails;
}
```

### Integration Tips:
- **Ticking Loop**: On every hourly simulator tick, loop through all firms. If `isOperational` is `false`, zero out `revenuePerHour` and scale down `upkeepCostPerHour` to 20% (representing basic security and property taxes).
- **Efficiency Scaling**: The `efficiency` parameter scales output production and sales velocities. Low efficiency increases waste and delays.
- **Triad Attributes**: Attributes like `Price`, `Quality`, and `Brand` (the triad) are calculated based on ingredients, technologies, and marketing and are matched against general city consumer indices to determine sales demand.

---

## 2. Interactive Mock Console Features (`index.html`)

You can launch `index.html` directly in any web browser to see the system in action. Key interactive features implemented:

1. **Live Sidebar Navigation**: Jump between 7 distinct mock buildings to see how the Central Workspace adjusts its view layout.
2. **Dynamic Canvas Sparklines**: Renders smooth trend lines representing hourly revenue, upkeep, and efficiency updates.
3. **3x3 Layout Matrix (Retail & Manufacturing)**:
   - Displays functional flow charts with animated arrow links.
   - Shows live triple bars (Supply, Demand, Utilization) inside each individual block.
   - Highlights block parameters in the inspector panel when clicked.
4. **Extraction View**: Features a circular SVG progress gauge detailing remaining reserves and exhaustion dates.
5. **Farm Tabs**: Toggles between Crops (with growth percentages) and Livestock (with feed quality modifiers adjusting upkeep and yields).
6. **R&D Laboratory**: Slider controls to adjust annual budgets (which calculates upkeep cost) and target durations.
7. **Real Estate Rentals**: Rent sliders reactively adjust tenancy occupancy rates (higher rent lowers occupancy, lower rent increases occupancy). Maintenance scores decay slowly, triggering red warnings and requiring clicked renovations.
8. **Suspend Operations**: Toggles operational state, dimming panels, zeroing output rates, and updating financial sparklines in real-time.
