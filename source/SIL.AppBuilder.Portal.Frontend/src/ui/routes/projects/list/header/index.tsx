import * as React from 'react';
import { Dropdown, Popup } from 'semantic-ui-react';
import { compose } from 'recompose';
import CaretDown from '@material-ui/icons/KeyboardArrowDown';

import DebouncedSearch from '@ui/components/inputs/debounced-search-field';
import { withTranslations, i18nProps } from '@lib/i18n';

import './styles.scss';
import { NavLink } from 'react-router-dom';

interface IOwnProps {
  filter: string;
  onSearch: (term: string) => any;
}

type IProps =
& IOwnProps
& i18nProps;

class Header extends React.Component<IProps> {

  render() {

    const { t, filter, onSearch } = this.props;

    const dropdownText = {
      'my-projects': t('projects.switcher.dropdown.myProjects'),
      'organization': t('projects.switcher.dropdown.orgProjects'),
      'archived': t('projects.switcher.dropdown.archived')
    };

    const trigger = (
      <>
        <div className='text'>{dropdownText[filter]}</div>
        <CaretDown />
      </>
    );

    return (
      <div className='flex justify-content-space-between p-t-md-xs p-b-md-xs'>
        <Dropdown
          className='project-switcher'
          trigger={trigger}
          icon={null}
          inline
        >
          <Dropdown.Menu>
            <Dropdown.Item text={t('projects.switcher.dropdown.myProjects')} as={NavLink} to='/projects/own'/>
            <Dropdown.Item text={t('projects.switcher.dropdown.orgProjects')} as={NavLink} to='/projects/organization' />
            <Dropdown.Item text={t('projects.switcher.dropdown.archived')} as={NavLink} to='/projects/archived' />
          </Dropdown.Menu>
        </Dropdown>
        <div className='flex align-items-center'>

          <Popup
            basic
            hoverable
            trigger={<div>
              <DebouncedSearch
                className='search-component'
                placeholder={t('common.search')}
                onSubmit={onSearch}
              />
            </div>}
            position='bottom center'>

            <div dangerouslySetInnerHTML={{ __html: t('directory.search-help') }} />

          </Popup>
        </div>
      </div>
    );
  }

}

export default compose<IOwnProps, IOwnProps>(
  withTranslations
)(Header);
