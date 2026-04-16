export type FieldType = 'text' | 'number' | 'boolean' | 'select';

export interface FieldSchema {
  key: string;
  label: string;
  type: FieldType;
  options?: string[];
  placeholder?: string;
}

export interface CategorySchema {
  label: string;
  icon: string; // Material Symbol name
  fields: FieldSchema[];
}

export const CATEGORY_SCHEMAS: Record<string, CategorySchema> = {
  Vehicle: {
    label: 'Vehicle',
    icon: 'directions_car',
    fields: [
      { key: 'Mileage', label: 'Mileage (km)', type: 'number', placeholder: 'e.g. 15000' },
      {
        key: 'Transmission',
        label: 'Transmission',
        type: 'select',
        options: ['Automatic', 'Manual'],
      },
      {
        key: 'FuelType',
        label: 'Fuel Type',
        type: 'select',
        options: ['Petrol', 'Diesel', 'Electric', 'Hybrid'],
      },
      { key: 'Seats', label: 'Number of Seats', type: 'number', placeholder: 'e.g. 5' },
    ],
  },
  Electronics: {
    label: 'Electronics',
    icon: 'devices',
    fields: [
      { key: 'Brand', label: 'Brand', type: 'text', placeholder: 'e.g. Sony' },
      { key: 'Model', label: 'Model', type: 'text', placeholder: 'e.g. Alpha A7 IV' },
    ],
  },
  RealEstate: {
    label: 'Real Estate',
    icon: 'home_work',
    fields: [
      { key: 'Bedrooms', label: 'Bedrooms (0 for studio)', type: 'number', placeholder: '0' },
      { key: 'Bathrooms', label: 'Bathrooms', type: 'number', placeholder: 'e.g. 1' },
      { key: 'MaxGuests', label: 'Max Guests', type: 'number', placeholder: 'e.g. 4' },
      { key: 'HasWifi', label: 'Wi-Fi Included', type: 'boolean' },
    ],
  },
  Sports: {
    label: 'Sports',
    icon: 'sports',
    fields: [
      { key: 'Sport', label: 'Sport / Activity', type: 'text', placeholder: 'e.g. Surfing, Cycling' },
      { key: 'Brand', label: 'Brand', type: 'text', placeholder: 'e.g. Trek' },
    ],
  },
  Tools: {
    label: 'Tools',
    icon: 'hardware',
    fields: [
      { key: 'Brand', label: 'Brand', type: 'text', placeholder: 'e.g. DeWalt' },
      { key: 'Voltage', label: 'Voltage', type: 'text', placeholder: 'e.g. 20V' },
      { key: 'Pieces', label: 'Pieces in Set', type: 'number', placeholder: 'e.g. 20' },
    ],
  },
  Outdoor: {
    label: 'Outdoor & Camping',
    icon: 'forest',
    fields: [
      { key: 'Capacity', label: 'Capacity (persons)', type: 'number', placeholder: 'e.g. 4' },
      {
        key: 'Season',
        label: 'Season Rating',
        type: 'select',
        options: ['1-season', '2-season', '3-season', '4-season'],
      },
      { key: 'WeightKg', label: 'Weight (kg)', type: 'number', placeholder: 'e.g. 2.8' },
    ],
  },
  Audio: {
    label: 'Audio & Music',
    icon: 'speaker',
    fields: [
      { key: 'Brand', label: 'Brand', type: 'text', placeholder: 'e.g. Bose' },
      { key: 'Watts', label: 'Watts', type: 'number', placeholder: 'e.g. 11' },
      { key: 'BatteryLife', label: 'Battery Life', type: 'text', placeholder: 'e.g. 11 hours' },
      { key: 'Channels', label: 'Channels', type: 'number', placeholder: 'e.g. 3' },
    ],
  },
  Other: {
    label: 'Other',
    icon: 'category',
    fields: [],
  },
};

export const CATEGORY_KEYS = Object.keys(CATEGORY_SCHEMAS);
