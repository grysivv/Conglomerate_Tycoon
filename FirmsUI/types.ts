/**
 * Capitalism Lab-Style Building Management (Firms) Data Model
 * 
 * Supports hierarchical sub-properties, functional block matrices,
 * financial histories, and complex sector-specific states.
 */

export enum BuildingType {
    RETAIL = "RETAIL",
    MANUFACTURING = "MANUFACTURING",
    EXTRACTION = "EXTRACTION",
    FARM = "FARM",
    RESEARCH = "RESEARCH",
    REAL_ESTATE = "REAL_ESTATE",
    HQ = "HQ"
}

export interface TriadAttributes {
    price: number;     // 0-100 score relative to market average
    quality: number;   // 0-100 score based on source ingredients/tech
    brand: number;     // 0-100 score driven by marketing/reputation
}

export enum BlockType {
    INPUT = "INPUT",
    PROCESSING = "PROCESSING",
    OUTPUT = "OUTPUT",
    INVENTORY = "INVENTORY",
    EMPTY = "EMPTY"
}

export interface FunctionalBlock {
    id: string;
    row: number;         // 0-2 (for 3x3 matrix)
    col: number;         // 0-2 (for 3x3 matrix)
    type: BlockType;
    name: string;
    supply: number;      // 0-100 percentage
    demand: number;      // 0-100 percentage
    utilization: number; // 0-100 percentage
    linkedBlockIds: string[]; // Connections in flow chart
    productName?: string;
    triad?: TriadAttributes;
}

export interface RetailDetails {
    inputBlockId: string;
    outputBlockId: string;
    profitMarginPercent: number;
    supplierFirmId?: string; // ID of supplier factory/warehouse
}

export interface ManufacturingDetails {
    inputBlockIds: string[];
    processingBlockId: string;
    outputBlockId: string;
    recipeName: string;      // E.g., "Desktop Computer"
    ingredientsList: { name: string; quantityNeeded: number; sourceQuality: number }[];
}

export interface ExtractionDetails {
    resourceName: string;    // E.g., "Iron Ore", "Crude Oil"
    remainingReserves: number; // In tons/barrels
    initialReserves: number;
    resourceQuality: number; // 0-100 score (inherent deposit quality)
    depletionRatePerHour: number;
}

export interface FarmDetails {
    activeTab: "CROPS" | "LIVESTOCK";
    cropType?: string;       // E.g., "Wheat", "Cotton"
    livestockType?: string;  // E.g., "Beef Cattle", "Pigs"
    growthProgress: number;  // 0-100 percentage
    breedingTimerHours: number;
    seasonalModifier: number; // 0.5x to 1.5x based on current month
}

export interface ResearchDetails {
    activeProjectName: string;
    projectCategory: string; // E.g., "Consumer Electronics"
    annualBudget: number;    // USD
    durationYears: number;   // 1 to 5 years
    techLevelProgress: number; // 0-100 percentage
    currentTechLevel: number;
}

export interface RealEstateDetails {
    occupancyRate: number;    // 0-100 percentage
    rentLevelIndex: number;   // 0-100 score (affects demand/occupancy)
    maintenanceScore: number; // 0-100 score (depreciates without spending)
    maintenanceAlertActive: boolean;
}

export interface HQDetails {
    departments: { name: string; staffCount: number; efficiencyBonusPercent: number }[];
    corporateWideBonusPercent: number;
}

export interface Firm {
    id: string;
    type: BuildingType;
    name: string;
    x: number;
    y: number;
    isOperational: boolean;
    efficiency: number;       // 0-100 percentage
    upkeepCostPerHour: number;
    revenuePerHour: number;
    netProfitPerHour: number;
    financialHistory: {
        timestamp: string;
        revenue: number;
        profit: number;
    }[];
    
    // 3x3 functional layout (primarily used by RETAIL & MANUFACTURING)
    layoutMatrix?: FunctionalBlock[]; 
    
    // Sector-specific properties (discriminated by type)
    retailDetails?: RetailDetails;
    manufacturingDetails?: ManufacturingDetails;
    extractionDetails?: ExtractionDetails;
    farmDetails?: FarmDetails;
    researchDetails?: ResearchDetails;
    realEstateDetails?: RealEstateDetails;
    hqDetails?: HQDetails;
}
