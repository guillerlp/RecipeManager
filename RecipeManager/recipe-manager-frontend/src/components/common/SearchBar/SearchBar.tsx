import styles from './SearchBar.module.css';
import { SearchIcon } from '@/components/ui/Icon';

interface SearchBarProps {
    searchQuery: string;
    onSearchChange: (query: string) => void;
    placeholder? : string;
}

export const SearchBar: React.FC<SearchBarProps> = ({
    searchQuery,
    onSearchChange,
    placeholder = "Search by name, ingredient or a word you remember"
}) => {

    const handleInputChange = (e:React.ChangeEvent<HTMLInputElement>) => {
        onSearchChange(e.target.value);
    }

    return (
        <div className={styles.searchContainer}>
            <div className={styles.searchInputWrapper}>
                <SearchIcon className={styles.searchIcon} />

                <input
                    type="text"
                    value={searchQuery}
                    onChange={handleInputChange}
                    placeholder={placeholder}
                    className={styles.searchInput}
                    aria-label="Search recipes"
                />
            </div>
        </div >
    );
}