import styles from './SearchBar.module.css';
import { SearchIcon } from '@/components/ui/Icon';

interface SearchBarProps {
    searchQuery: string;
    onSearchChange: (query: string) => void;
    placeholder? : string;
    // React 19: ref is an ordinary prop on a function component, so the input can be focused from outside.
    ref?: React.Ref<HTMLInputElement>;
}

export const SearchBar: React.FC<SearchBarProps> = ({
    searchQuery,
    onSearchChange,
    placeholder = "Search by name, ingredient or a word you remember",
    ref
}) => {

    const handleInputChange = (e:React.ChangeEvent<HTMLInputElement>) => {
        onSearchChange(e.target.value);
    }

    return (
        <div className={styles.searchContainer}>
            <div className={styles.searchInputWrapper}>
                <SearchIcon className={styles.searchIcon} />

                <input
                    ref={ref}
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